﻿using Calamari.Commands.Support;
using Calamari.Integration.FileSystem;
using Calamari.Integration.Processes;

namespace Calamari.Terraform
{
    [Command("destroy-terraform", Description = "Destroys Terraform resources")]
    public class DestroyCommand : TerraformCommand
    {
        public DestroyCommand(CalamariVariableDictionary variables, ICalamariFileSystem fileSystem)
            : base(variables, fileSystem, new DestroyTerraformConvention(fileSystem))
        {
        }
    }
}