#region Copyright 
// Copyright 2017 Gygya Inc.  All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License.  
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDER AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED.  IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
#endregion

using System;
using System.Linq;
using Hyperscale.Microcore.Interfaces.Configuration;
using Hyperscale.Microcore.Interfaces.SystemWrappers;
using Hyperscale.Microcore.SharedLogic.Exceptions;
using Newtonsoft.Json.Linq;

namespace Hyperscale.Microcore.Configuration
{
    /// <summary>
    /// Will try to check if file C:\hs\environmentVariables.json exists 
    /// and will try to read content it expects a single object where each property is a name of environment variable
    /// {
    ///     "EnvVar1":"Value",
    ///     "EnvVar1":"Value"
    /// }
    /// </summary>
    public class EnvironmentVariablesFileReader
    {
        private const string HS_ENV_VARS_FILE = "HS_ENVVARS_FILE";
        private const string ENV_FILEPATH = "{0}/hs/environmentVariables.json";
        private readonly string locEnvFilePath;

        private IEnvironmentVariableProvider EnvironmentVariableProvider { get; }
        private IFileSystem FileSystem { get; }

        /// <summary>
        /// Parses the content of environment variables file content.
        /// </summary>        
        public EnvironmentVariablesFileReader(IFileSystem fileSystem, IEnvironmentVariableProvider environmentVariableProvider)
        {
            locEnvFilePath = environmentVariableProvider.GetEnvironmentVariable(HS_ENV_VARS_FILE);

            if (string.IsNullOrEmpty(locEnvFilePath))
            {
                locEnvFilePath = string.Format(ENV_FILEPATH, environmentVariableProvider.PlatformSpecificPathPrefix);
            }

            EnvironmentVariableProvider = environmentVariableProvider;
            FileSystem = fileSystem;
        }


        /// <summary>
        /// Reads each property in file and sets its environment variable.
        /// </summary>
        /// <returns>Names of environment variables read from file</returns>
        public void ReadFromFile()
        {
            JObject envVarsObject;

            try
            {
                var text = FileSystem.TryReadAllTextFromFile(locEnvFilePath);

                if (string.IsNullOrEmpty(text))
                    return;

                envVarsObject = JObject.Parse(text);
            }
            catch (Exception ex)
            {
                throw new ConfigurationException($"Missing or invalid configuration file: {locEnvFilePath}", ex);
            }

            if (envVarsObject == null)
                return;

            var properties = envVarsObject.Properties().Where(a => a.HasValues).ToArray();

            foreach (var property in properties)
            {
                 EnvironmentVariableProvider.SetEnvironmentVariableForProcess(property.Name, property.Value.Value<string>());
            }
        }
    }
}