using System;
using Extensibility;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.CommandBars;
using System.Resources;
using System.Reflection;
using System.Globalization;
using System.IO;

namespace VisualStudioSnoopAddin
{
	/// <summary>The object for implementing an Add-in.</summary>
	/// <seealso class='IDTExtensibility2' />
	public class Connect : IDTExtensibility2, IDTCommandTarget
	{
		/// <summary>Implements the constructor for the Add-in object. Place your initialization code within this method.</summary>
		public Connect()
		{
		}

		/// <summary>Implements the OnConnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being loaded.</summary>
		/// <param term='application'>Root object of the host application.</param>
		/// <param term='connectMode'>Describes how the Add-in is being loaded.</param>
		/// <param term='addInInst'>Object representing this Add-in.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
		{
			_applicationObject = (DTE2)application;
			_addInInstance = (AddIn)addInInst;
			if(connectMode == ext_ConnectMode.ext_cm_UISetup)
			{
				object []contextGUIDS = new object[] { };
				Commands2 commands = (Commands2)_applicationObject.Commands;
				string toolsMenuName = "Tools";

				//Place the command on the tools menu.
				//Find the MenuBar command bar, which is the top-level command bar holding all the main menu items:
				Microsoft.VisualStudio.CommandBars.CommandBar menuBarCommandBar = ((Microsoft.VisualStudio.CommandBars.CommandBars)_applicationObject.CommandBars)["MenuBar"];

				//Find the Tools command bar on the MenuBar command bar:
				CommandBarControl toolsControl = menuBarCommandBar.Controls[toolsMenuName];
				CommandBarPopup toolsPopup = (CommandBarPopup)toolsControl;

				//This try/catch block can be duplicated if you wish to add multiple commands to be handled by your Add-in,
				//  just make sure you also update the QueryStatus/Exec method to include the new command names.
				try
				{
					//Add a command to the Commands collection:
					Command command = commands.AddNamedCommand2(_addInInstance, "VisualStudioSnoopAddin", "Snoop Application", "Executes the command for VisualStudioSnoopAddin", false, 1, ref contextGUIDS, (int)vsCommandStatus.vsCommandStatusSupported+(int)vsCommandStatus.vsCommandStatusEnabled, (int)vsCommandStyle.vsCommandStylePictAndText, vsCommandControlType.vsCommandControlTypeButton);
                    command.Bindings = "Global::Shift+Ctrl+Z";

					//Add a control for the command to the tools menu:
					if((command != null) && (toolsPopup != null))
					{
						command.AddControl(toolsPopup.CommandBar, 1);
					}
				}
				catch(System.ArgumentException)
				{
					//If we are here, then the exception is probably because a command with that name
					//  already exists. If so there is no need to recreate the command and we can 
                    //  safely ignore the exception.
				}
			}
		}

		/// <summary>Implements the OnDisconnection method of the IDTExtensibility2 interface. Receives notification that the Add-in is being unloaded.</summary>
		/// <param term='disconnectMode'>Describes how the Add-in is being unloaded.</param>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnDisconnection(ext_DisconnectMode disconnectMode, ref Array custom)
		{
		}

		/// <summary>Implements the OnAddInsUpdate method of the IDTExtensibility2 interface. Receives notification when the collection of Add-ins has changed.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />		
		public void OnAddInsUpdate(ref Array custom)
		{
		}

		/// <summary>Implements the OnStartupComplete method of the IDTExtensibility2 interface. Receives notification that the host application has completed loading.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnStartupComplete(ref Array custom)
		{
		}

		/// <summary>Implements the OnBeginShutdown method of the IDTExtensibility2 interface. Receives notification that the host application is being unloaded.</summary>
		/// <param term='custom'>Array of parameters that are host application specific.</param>
		/// <seealso class='IDTExtensibility2' />
		public void OnBeginShutdown(ref Array custom)
		{
		}
		
		/// <summary>Implements the QueryStatus method of the IDTCommandTarget interface. This is called when the command's availability is updated</summary>
		/// <param term='commandName'>The name of the command to determine state for.</param>
		/// <param term='neededText'>Text that is needed for the command.</param>
		/// <param term='status'>The state of the command in the user interface.</param>
		/// <param term='commandText'>Text requested by the neededText parameter.</param>
		/// <seealso class='Exec' />
		public void QueryStatus(string commandName, vsCommandStatusTextWanted neededText, ref vsCommandStatus status, ref object commandText)
		{
			if(neededText == vsCommandStatusTextWanted.vsCommandStatusTextWantedNone)
			{
				if(commandName == "VisualStudioSnoopAddin.Connect.VisualStudioSnoopAddin")
				{
					status = (vsCommandStatus)vsCommandStatus.vsCommandStatusSupported|vsCommandStatus.vsCommandStatusEnabled;
					return;
				}
			}
		}

        static bool Is64BitProcess(System.Diagnostics.Process process)
        {
            try
            {
                var processes = process.Modules;
                return false;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return true;
            }
        }

        static string Suffix(System.Diagnostics.Process process)
        {
            if (Is64BitProcess(process))
                return "64-4.0";

            string bitness = IntPtr.Size == 8 ? "64" : "32";
            string clr = "3.5";


            //var assembly = System.Reflection.Assembly.ReflectionOnlyLoad("");

            foreach (System.Diagnostics.ProcessModule module in process.Modules)//window.Modules)
            {
                // a process is valid to snoop if it contains a dependency on PresentationFramework, PresentationCore, or milcore (wpfgfx).
                // this includes the files:
                // PresentationFramework.dll, PresentationFramework.ni.dll
                // PresentationCore.dll, PresentationCore.ni.dll
                // wpfgfx_v0300.dll (WPF 3.0/3.5)
                // wpfgrx_v0400.dll (WPF 4.0)

                // note: sometimes PresentationFramework.dll doesn't show up in the list of modules.
                // so, it makes sense to also check for the unmanaged milcore component (wpfgfx_vxxxx.dll).
                // see for more info: http://snoopwpf.codeplex.com/Thread/View.aspx?ThreadId=236335

                // sometimes the module names aren't always the same case. compare case insensitive.
                // see for more info: http://snoopwpf.codeplex.com/workitem/6090

                if
                (
                    module.FileName.Contains("PresentationFramework") ||
                    module.FileName.Contains("PresentationCore") ||
                    module.FileName.Contains("wpfgfx")
                )
                {
                    if (module.FileVersionInfo.FileMajorPart > 3)
                    {
                        clr = "4.0";
                    }
                }

                if (module.FileName.Contains("wow64.dll"))
                {
                    if (module.FileVersionInfo.FileMajorPart > 3)
                    {
                        bitness = "32";
                    }
                }
            }
            return bitness + "-" + clr;
        }


		/// <summary>Implements the Exec method of the IDTCommandTarget interface. This is called when the command is invoked.</summary>
		/// <param term='commandName'>The name of the command to execute.</param>
		/// <param term='executeOption'>Describes how the command should be run.</param>
		/// <param term='varIn'>Parameters passed from the caller to the command handler.</param>
		/// <param term='varOut'>Parameters passed from the command handler to the caller.</param>
		/// <param term='handled'>Informs the caller if the command was handled or not.</param>
		/// <seealso class='Exec' />
		public void Exec(string commandName, vsCommandExecOption executeOption, ref object varIn, ref object varOut, ref bool handled)
		{
			handled = false;
			if(executeOption == vsCommandExecOption.vsCommandExecOptionDoDefault)
			{
				if(commandName == "VisualStudioSnoopAddin.Connect.VisualStudioSnoopAddin")
				{
					handled = true;

                    foreach (var x in _applicationObject.Debugger.DebuggedProcesses)
                    {
                        //((EnvDTE.Process)x).

                        //System.Diagnostics.Debug.WriteLine(System.Diagnostics.Process.GetProcessById(((EnvDTE.Process)x).ProcessID).MainWindowHandle);
                        var windowProcess = System.Diagnostics.Process.GetProcessById(((EnvDTE.Process)x).ProcessID);
                        var mainWindowHandle = windowProcess.MainWindowHandle;

                        string programFilesFolder = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                        string snoopDirectory = Path.Combine(programFilesFolder, "Snoop");
                        string filename = "ManagedInjectorLauncher" + Suffix(windowProcess) + ".exe";
                        string fullFileName = Path.Combine(snoopDirectory, filename);

                        string snoopExePath = Path.Combine(snoopDirectory, "Snoop.exe");
                        string className = "Snoop.SnoopUI";
                        string methodName = "GoBabyGo";

                        System.Diagnostics.Process.Start(fullFileName, mainWindowHandle + " \"" + snoopExePath + "\" \"" + className + "\" \"" + methodName + "\"");

                    }
					return;
				}
			}
		}
		private DTE2 _applicationObject;
		private AddIn _addInInstance;
	}
}