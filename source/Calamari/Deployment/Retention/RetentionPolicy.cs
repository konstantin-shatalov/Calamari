﻿using System;
using System.Collections.Generic;
using System.Linq;
using Calamari.Common.Features.Deployment.Journal;
using Calamari.Common.Plumbing.FileSystem;
using Calamari.Common.Plumbing.Logging;
using Calamari.Integration.Packages.Download;
using Calamari.Integration.Time;

namespace Calamari.Deployment.Retention
{
    public class RetentionPolicy : IRetentionPolicy
    {
        private static readonly IPackageDownloaderUtils PackageDownloaderUtils = new PackageDownloaderUtils();
        readonly ICalamariFileSystem fileSystem;
        readonly IDeploymentJournal deploymentJournal;
        readonly IClock clock;

        public RetentionPolicy(ICalamariFileSystem fileSystem, IDeploymentJournal deploymentJournal, IClock clock)
        {
            this.fileSystem = fileSystem;
            this.deploymentJournal = deploymentJournal;
            this.clock = clock;
        }

        public void ApplyRetentionPolicy(string retentionPolicySet, int? days, int? releases)
        {
            var deployments = deploymentJournal
                .GetAllJournalEntries()
                .Where(x => x.RetentionPolicySet == retentionPolicySet)
                .ToList();
            var preservedEntries = new List<JournalEntry>();

            if (days.HasValue && days.Value > 0)
            {
                Log.Info($"Keeping deployments from the last {days} days");
                deployments = deployments
                    .Where(InstalledBeforePolicyDayCutoff(days.Value, preservedEntries))
                    .ToList();
            }
            else if (releases.HasValue && releases.Value > 0)
            {
                Log.Info($"Keeping this deployment and the previous {releases} successful deployments");
                // Keep the current release, plus specified releases value
                // Unsuccessful releases are not included in the count of releases to keep
                deployments = deployments
                    .OrderByDescending(deployment => deployment.InstalledOn)
                    .SkipWhile(SuccessfulCountLessThanPolicyCount(releases.Value, preservedEntries))
                    .ToList();
            }
            else
            {
                Log.Info($"Keeping all releases");
                return;
            }

            if (!deployments.Any())
            {
                Log.Info("Did not find any deployments to clean up");
            }

            foreach (var deployment in deployments)
            {
                DeleteExtractionDestination(deployment, preservedEntries);

                foreach (var package in deployment.Packages)
                {
                    DeleteExtractionSource(package, preservedEntries);
                }
            }
            deploymentJournal.RemoveJournalEntries(deployments.Select(x => x.Id));

            RemovedFailedPackageDownloads();
        }

        void DeleteExtractionSource(DeployedPackage deployedPackage, List<JournalEntry> preservedEntries)
        {
            if (string.IsNullOrWhiteSpace(deployedPackage.DeployedFrom)
                || !fileSystem.FileExists(deployedPackage.DeployedFrom)
                || preservedEntries.Any(entry => entry.Packages.Any(p => deployedPackage.DeployedFrom.Equals(p.DeployedFrom, StringComparison.Ordinal))))
                return;

            Log.Info($"Removing package file '{deployedPackage.DeployedFrom}'");
            fileSystem.DeleteFile(deployedPackage.DeployedFrom, FailureOptions.IgnoreFailure);
        }

        void DeleteExtractionDestination(JournalEntry deployment, List<JournalEntry> preservedEntries)
        {
            if (string.IsNullOrWhiteSpace(deployment.ExtractedTo)
                || !fileSystem.DirectoryExists(deployment.ExtractedTo)
                || preservedEntries.Any(entry => deployment.ExtractedTo.Equals(entry.ExtractedTo, StringComparison.Ordinal)))
                return;

            Log.Info($"Removing directory '{deployment.ExtractedTo}'");
            fileSystem.PurgeDirectory(deployment.ExtractedTo, FailureOptions.IgnoreFailure);

            try
            {
                fileSystem.DeleteDirectory(deployment.ExtractedTo);
            }
            catch (Exception ex)
            {
                Log.VerboseFormat("Could not delete directory '{0}' because some files could not be deleted: {1}",
                    deployment.ExtractedTo, ex.Message);
            }
        }

        static Func<JournalEntry, bool> SuccessfulCountLessThanPolicyCount(int releases, List<JournalEntry> preservedEntries)
        {
            return journalEntry =>
            {
                if (preservedEntries.Count() > releases)
                {
                    return false;
                }

                if (journalEntry.WasSuccessful)
                {
                    preservedEntries.Add(journalEntry);
                    
                    var preservedDirectories = (!string.IsNullOrEmpty(journalEntry.ExtractedTo)
                            ? new List<string> {journalEntry.ExtractedTo}
                            : new List<string>())
                        .Concat(journalEntry.Packages.Select(p => p.DeployedFrom).Where(d => !string.IsNullOrEmpty(d)))
                        .ToList();
                    
                    Log.Verbose($"Keeping {FormatList(preservedDirectories)} as it is the {FormatWithThPostfix(preservedEntries.Count)}most recent successful release");
                }
                return true;
            };
        }

        Func<JournalEntry, bool> InstalledBeforePolicyDayCutoff(int days, List<JournalEntry> preservedEntries)
        {
            return journalEntry =>
            {
                var installedAgo = (clock.GetUtcTime() - journalEntry.InstalledOn);

                if (journalEntry.InstalledOn == null)
                    return false;
                if (installedAgo?.TotalDays > days)
                    return true;
                
                var preservedDirectories = (!string.IsNullOrEmpty(journalEntry.ExtractedTo)
                        ? new List<string> {journalEntry.ExtractedTo}
                        : new List<string>())
                    .Concat(journalEntry.Packages.Select(p => p.DeployedFrom).Where(p => !string.IsNullOrEmpty(p)))
                    .ToList();

                Log.Verbose($"Keeping {FormatList(preservedDirectories)} as it was installed {installedAgo?.Days} days and {installedAgo?.Hours} hours ago");

                preservedEntries.Add(journalEntry);
                return false;
            };
        }
        
        static string FormatList(IList<string> items)
        {
            if (items.Count <= 1)
                return $"{items.FirstOrDefault() ?? ""}";

            return string.Join(", ", items.Take(items.Count - 1)) + $" and {items[items.Count - 1]}";
        }

        static string FormatWithThPostfix(int value)
        {
            if (value == 1)
                return "";

            if (value == 11 || value == 12 || value == 13)
                return value + "th ";

            switch (value % 10)
            {
                case 1:
                    return value + "st ";
                case 2:
                    return value + "nd ";
                case 3:
                    return value + "rd ";
                default:
                    return value + "th ";
            }
        }

        private void RemovedFailedPackageDownloads()
        {
            var pattern = "*" + NuGetPackageDownloader.DownloadingExtension;

            if (fileSystem.DirectoryExists(PackageDownloaderUtils.RootDirectory))
            {
                var toDelete = fileSystem.EnumerateFilesRecursively(PackageDownloaderUtils.RootDirectory, pattern)
                    .Where(f => fileSystem.GetCreationTime(f) <= DateTime.Now.AddDays(-1))
                    .ToArray();

                foreach (var file in toDelete)
                {
                    Log.Verbose($"Removing the failed to download file {file}");
                    fileSystem.DeleteFile(file, FailureOptions.IgnoreFailure);
                }
            }
        }
    }
}