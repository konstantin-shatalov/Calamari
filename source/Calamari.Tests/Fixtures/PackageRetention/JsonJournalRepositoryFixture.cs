﻿using System.IO;
using Calamari.Common.Features.Processes.Semaphores;
using Calamari.Common.Plumbing.FileSystem;
using Calamari.Common.Plumbing.Variables;
using Calamari.Deployment.PackageRetention.Repositories;
using Calamari.Tests.Fixtures.Integration.FileSystem;
using Calamari.Tests.Helpers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Calamari.Tests.Fixtures.PackageRetention
{
    [TestFixture]
    public class JsonJournalRepositoryFixture
    {
        [Test]
        public void WhenCalamariPackageRetentionJournalPathExists_ThenTheJournalIsCreatedAtTheGivenPath()
        {
            var journalPath = TestEnvironment.GetTestPath("PackageRetentionJournal.json");

            var variables = Substitute.For<IVariables>();
            variables.Get(KnownVariables.Calamari.PackageRetentionJournalPath).Returns(journalPath);

            var repository = new JsonJournalRepository(TestCalamariPhysicalFileSystem.GetPhysicalFileSystem(), Substitute.For<ISemaphoreFactory>(), variables);

            repository.Commit();
            Assert.IsTrue(File.Exists(journalPath));
        }

        [Test]
        public void WhenCalamariPackageRetentionJournalPathDoesNotExist_ThenTheJournalIsCreatedAtTheDefaultPath()
        {
            var homeDir = TestEnvironment.GetTestPath();

            var variables = Substitute.For<IVariables>();
            variables.Get(KnownVariables.Calamari.PackageRetentionJournalPath).Returns((string) null);
            variables.Get(TentacleVariables.Agent.TentacleHome).Returns(homeDir);

            var repository = new JsonJournalRepository(Substitute.For<ICalamariFileSystem>(), Substitute.For<ISemaphoreFactory>(), variables);

            var expectedPath = Path.Combine(homeDir, JsonJournalRepository.DefaultJournalName);

            repository.Commit();
            Assert.IsTrue(File.Exists(expectedPath));
        }
    }
}