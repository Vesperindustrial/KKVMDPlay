using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace KKVMDPlayPlugin
{
	
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
	[DebuggerNonUserCode]
	[CompilerGenerated]
	internal class VMDResources
	{
		
		internal VMDResources()
		{
		}

		
		
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (VMDResources.resourceMan == null)
				{
					VMDResources.resourceMan = new ResourceManager("KKVMDPlayPlugin.VMDResources", typeof(VMDResources).Assembly);
				}
				return VMDResources.resourceMan;
			}
		}

		
		
		
		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get
			{
				return VMDResources.resourceCulture;
			}
			set
			{
				VMDResources.resourceCulture = value;
			}
		}

		
		
		internal static byte[] file_icon
		{
			get
			{
				return (byte[])VMDResources.ResourceManager.GetObject("file_icon", VMDResources.resourceCulture);
			}
		}

		
		
		internal static byte[] folder_icon
		{
			get
			{
				return (byte[])VMDResources.ResourceManager.GetObject("folder_icon", VMDResources.resourceCulture);
			}
		}

		
		private static ResourceManager resourceMan;

		
		private static CultureInfo resourceCulture;
	}
}
