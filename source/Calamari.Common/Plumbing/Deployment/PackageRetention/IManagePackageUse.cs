﻿using System;

namespace Calamari.Common.Plumbing.Deployment.PackageRetention
{
    public interface IManagePackageUse
    {
        void RegisterPackageUse(PackageIdentity package, ServerTaskId deploymentTaskId, long packageSizeBytes);
        void DeregisterPackageUse(PackageIdentity package, ServerTaskId serverTaskId);
        void ApplyRetention(string packageDirectory);
        void ExpireStaleLocks(TimeSpan timeBeforeExpiration);
    }
}