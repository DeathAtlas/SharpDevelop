using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.Remoting;
using System.Security.Policy;

namespace ICSharpCode.SharpDevelop.Services
{
	[Serializable]
	class RemotingConfigurationHelpper
	{
		public string path;

		public RemotingConfigurationHelpper(string path)
		{
			this.path = path;
		}

		public static string GetLoadedAssemblyPath(string assemblyName)
		{
			string path = null;
			foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				string fullFilename = assembly.Location;
				if (Path.GetFileName(fullFilename).ToLower() == assemblyName.ToLower()) {
					path = Path.GetDirectoryName(fullFilename);
					break;
				}
			}
			if (path == null) {
				throw new System.Exception("Assembly " + assemblyName + " is not loaded");
			}
			return path;
		}

		public void Configure()
		{
			AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
			
			RemotingConfiguration.Configure(Path.Combine(path, "Client.config"));

			string baseDir = Directory.GetDirectoryRoot(AppDomain.CurrentDomain.BaseDirectory);
			string relDirs = AppDomain.CurrentDomain.BaseDirectory + ";" + path;
			AppDomain serverAppDomain = AppDomain.CreateDomain("Debugging server",
				                                                new Evidence(AppDomain.CurrentDomain.Evidence),
																baseDir,
																relDirs,
																AppDomain.CurrentDomain.ShadowCopyFiles);
			serverAppDomain.DoCallBack(new CrossAppDomainDelegate(ConfigureServer));
		}

		private void ConfigureServer()
		{
			AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
			RemotingConfiguration.Configure(Path.Combine(path, "Server.config"));
		}

		Assembly AssemblyResolve(object sender, ResolveEventArgs args)
		{
			foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				string fullFilename = assembly.Location;
				if (Path.GetFileNameWithoutExtension(fullFilename).ToLower() == args.Name.ToLower() ||
					assembly.FullName == args.Name) {
					return assembly;
				}
			}
			return null;
		}
	}
}
