using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;             
using System.Runtime.InteropServices;   
using System.IO;
using Microsoft.Win32.SafeHandles;     

namespace Dolinay
{
    internal class DetectorForm : Form
    {
        private Label label1;
        private DriveDetector mDetector = null;
        public DetectorForm(DriveDetector detector)
        {
            mDetector = detector;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            this.ShowIcon = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Load += new System.EventHandler(this.Load_Form);
            this.Activated += new EventHandler(this.Form_Activated);
        }

        private void Load_Form(object sender, EventArgs e)
        {
            InitializeComponent();
            this.Size = new System.Drawing.Size(5, 5);
        }

        private void Form_Activated(object sender, EventArgs e)
        {
            this.Visible = false;
        }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (mDetector != null)
            {
                mDetector.WndProc(ref m);
            }
        }

        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(314, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "This is invisible form. To see DriveDetector code click View Code";
            this.ClientSize = new System.Drawing.Size(360, 80);
            this.Controls.Add(this.label1);
            this.Name = "DetectorForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }   

    public delegate void DriveDetectorEventHandler(Object sender, DriveDetectorEventArgs e);
    public class DriveDetectorEventArgs : EventArgs 
    {


        public DriveDetectorEventArgs()
        {
            Cancel = false;
            Drive = "";
            HookQueryRemove = false;
        }
        public bool Cancel;
        public string Drive;
        public bool HookQueryRemove;

    }

    class DriveDetector : IDisposable 
    {
        public event DriveDetectorEventHandler DeviceArrived;
        public event DriveDetectorEventHandler DeviceRemoved;
        public event DriveDetectorEventHandler QueryRemove;

        public DriveDetector()
        {
            DetectorForm  frm = new DetectorForm(this);
            frm.Show(); // will be hidden immediatelly
            Init(frm, null);
        }

        public DriveDetector(Control control)
        {
            Init(control, null);
        }

        public DriveDetector(Control control, string FileToOpen)
        {
            Init(control, FileToOpen);
        }

        private void Init(Control control, string fileToOpen)
        {
            mFileToOpen = fileToOpen;
            mFileOnFlash = null;
            mDeviceNotifyHandle = IntPtr.Zero;
            mRecipientHandle = control.Handle;
            mDirHandle = IntPtr.Zero;  
            mCurrentDrive = "";
        }

	    public bool IsQueryHooked
	    {
		    get
            {
                if (mDeviceNotifyHandle == IntPtr.Zero)
                    return false;
                else
                    return true;
            }
	    }

        public string HookedDrive
        {
            get
            {
                return mCurrentDrive;
            }
        }

        public FileStream OpenedFile
        {
            get
            {
                return mFileOnFlash;
            }
        }

        public bool EnableQueryRemove(string fileOnDrive)
        {
            if (fileOnDrive == null || fileOnDrive.Length == 0)
                throw new ArgumentException("Drive path must be supplied to register for Query remove.");
            
            if ( fileOnDrive.Length == 2 && fileOnDrive[1] == ':' )
                fileOnDrive += '\\';  

            if (mDeviceNotifyHandle != IntPtr.Zero)
            {
                RegisterForDeviceChange(false, null);
            }

            if (Path.GetFileName(fileOnDrive).Length == 0 ||!File.Exists(fileOnDrive))
                mFileToOpen = null;    
            else
                mFileToOpen = fileOnDrive;

            RegisterQuery(Path.GetPathRoot(fileOnDrive));
            if (mDeviceNotifyHandle == IntPtr.Zero)
                return false;  

            return true;
        }

        public void DisableQueryRemove()
        {
            if (mDeviceNotifyHandle != IntPtr.Zero)
            {
                RegisterForDeviceChange(false, null);
            }
        }


        public void Dispose()
        {
            RegisterForDeviceChange(false, null);
        }


        #region WindowProc
        public void WndProc(ref Message m)
        {
            int devType;
            char c;

            if (m.Msg == WM_DEVICECHANGE)
            {
                switch (m.WParam.ToInt32())
                {
                    case DBT_DEVICEARRIVAL:

                        devType = Marshal.ReadInt32(m.LParam, 4);
                        if (devType == DBT_DEVTYP_VOLUME)
                        {
                            DEV_BROADCAST_VOLUME vol;
                            vol = (DEV_BROADCAST_VOLUME)
                                Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_VOLUME));

                            c = DriveMaskToLetter(vol.dbcv_unitmask);

                            DriveDetectorEventHandler tempDeviceArrived = DeviceArrived;
                            if ( tempDeviceArrived != null )
                            {
                                DriveDetectorEventArgs e = new DriveDetectorEventArgs();
                                e.Drive = c + ":\\";
                                tempDeviceArrived(this, e);

                                if (e.HookQueryRemove)
                                {
                                    if (mDeviceNotifyHandle != IntPtr.Zero)
                                    {
                                        RegisterForDeviceChange(false, null);
                                    }
                                        
                                   RegisterQuery(c + ":\\");
                                }
                            }     
                        }
                        break;

                    case DBT_DEVICEQUERYREMOVE:

                        devType = Marshal.ReadInt32(m.LParam, 4);
                        if (devType == DBT_DEVTYP_HANDLE)
                        {
                            DriveDetectorEventHandler tempQuery = QueryRemove;
                            if (tempQuery != null)
                            {
                                DriveDetectorEventArgs e = new DriveDetectorEventArgs();
                                e.Drive = mCurrentDrive;        // drive which is hooked
                                tempQuery(this, e);

                                if (e.Cancel)
                                {                                    
                                    m.Result = (IntPtr)BROADCAST_QUERY_DENY;
                                }
                                else
                                {
                                    RegisterForDeviceChange(false, null);   
                                }
                           }                          
                        }
                        break;

                    case DBT_DEVICEREMOVECOMPLETE:

                        devType = Marshal.ReadInt32(m.LParam, 4);
                        if (devType == DBT_DEVTYP_VOLUME)
                        {
                            devType = Marshal.ReadInt32(m.LParam, 4);
                            if (devType == DBT_DEVTYP_VOLUME)
                            {
                                DEV_BROADCAST_VOLUME vol;
                                vol = (DEV_BROADCAST_VOLUME)
                                    Marshal.PtrToStructure(m.LParam, typeof(DEV_BROADCAST_VOLUME));
                                c = DriveMaskToLetter(vol.dbcv_unitmask);

                                DriveDetectorEventHandler tempDeviceRemoved = DeviceRemoved;
                                if (tempDeviceRemoved != null)
                                {
                                    DriveDetectorEventArgs e = new DriveDetectorEventArgs();
                                    e.Drive = c + ":\\";
                                    tempDeviceRemoved(this, e);
                                }
                            }
                        }
                        break;
                }

            }

        }
        #endregion



        #region  Private Area

        /// <summary>
        /// New: 28.10.2007 - handle to root directory of flash drive which is opened
        /// for device notification
        /// </summary>
        private IntPtr mDirHandle = IntPtr.Zero;

        /// <summary>
        /// Class which contains also handle to the file opened on the flash drive
        /// </summary>
        private FileStream mFileOnFlash = null;

        /// <summary>
        /// Name of the file to try to open on the removable drive for query remove registration
        /// </summary>
        private string mFileToOpen;

        /// <summary>
        /// Handle to file which we keep opened on the drive if query remove message is required by the client
        /// </summary>       
        private IntPtr mDeviceNotifyHandle;

        /// <summary>
        /// Handle of the window which receives messages from Windows. This will be a form.
        /// </summary>
        private IntPtr mRecipientHandle;

        /// <summary>
        /// Drive which is currently hooked for query remove
        /// </summary>
        private string mCurrentDrive;   


        // Win32 constants
        private const int DBT_DEVTYP_DEVICEINTERFACE = 5;
        private const int DBT_DEVTYP_HANDLE = 6;
        private const int BROADCAST_QUERY_DENY = 0x424D5144;
        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000; // system detected a new device
        private const int DBT_DEVICEQUERYREMOVE = 0x8001;   // Preparing to remove (any program can disable the removal)
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004; // removed 
        private const int DBT_DEVTYP_VOLUME = 0x00000002; // drive type is logical volume

        /// <summary>
        /// Registers for receiving the query remove message for a given drive.
        /// We need to open a handle on that drive and register with this handle. 
        /// Client can specify this file in mFileToOpen or we will open root directory of the drive
        /// </summary>
        /// <param name="drive">drive for which to register. </param>
        private void RegisterQuery(string drive)
        {
            bool register = true;

            if (mFileToOpen == null)
            {
                // Change 28.10.2007 - Open the root directory if no file specified - leave mFileToOpen null 
                // If client gave us no file, let's pick one on the drive... 
                //mFileToOpen = GetAnyFile(drive);
                //if (mFileToOpen.Length == 0)
                //    return;     // no file found on the flash drive                
            }
            else
            {
                // Make sure the path in mFileToOpen contains valid drive
                // If there is a drive letter in the path, it may be different from the  actual
                // letter assigned to the drive now. We will cut it off and merge the actual drive 
                // with the rest of the path.
                if (mFileToOpen.Contains(":"))
                {
                    string tmp = mFileToOpen.Substring(3);
                    string root = Path.GetPathRoot(drive);
                    mFileToOpen = Path.Combine(root, tmp);
                }
                else
                    mFileToOpen = Path.Combine(drive, mFileToOpen);
            }


            try
            {
                //mFileOnFlash = new FileStream(mFileToOpen, FileMode.Open);
                // Change 28.10.2007 - Open the root directory 
                if (mFileToOpen == null)  // open root directory
                    mFileOnFlash = null;
                else
                    mFileOnFlash = new FileStream(mFileToOpen, FileMode.Open);
            }
            catch (Exception)
            {
                // just do not register if the file could not be opened
                register = false;
            }


            if (register)
            {
                //RegisterForDeviceChange(true, mFileOnFlash.SafeFileHandle);
                //mCurrentDrive = drive;
                // Change 28.10.2007 - Open the root directory 
                if (mFileOnFlash == null)
                    RegisterForDeviceChange(drive);
                else
                    // old version
                    RegisterForDeviceChange(true, mFileOnFlash.SafeFileHandle);

                mCurrentDrive = drive;
            }


        }


        /// <summary>
        /// New version which gets the handle automatically for specified directory
        /// Only for registering! Unregister with the old version of this function...
        /// </summary>
        /// <param name="register"></param>
        /// <param name="dirPath">e.g. C:\\dir</param>
        private void RegisterForDeviceChange(string dirPath)
        {
            IntPtr handle = Native.OpenDirectory(dirPath);
            if (handle == IntPtr.Zero)
            {
                mDeviceNotifyHandle = IntPtr.Zero;
                return;
            }
            else
                mDirHandle = handle;    // save handle for closing it when unregistering

            // Register for handle
            DEV_BROADCAST_HANDLE data = new DEV_BROADCAST_HANDLE();
            data.dbch_devicetype = DBT_DEVTYP_HANDLE;
            data.dbch_reserved = 0;
            data.dbch_nameoffset = 0;
            //data.dbch_data = null;
            //data.dbch_eventguid = 0;
            data.dbch_handle = handle;
            data.dbch_hdevnotify = (IntPtr)0;
            int size = Marshal.SizeOf(data);
            data.dbch_size = size;
            IntPtr buffer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, buffer, true);

            mDeviceNotifyHandle = Native.RegisterDeviceNotification(mRecipientHandle, buffer, 0);

        }

        /// <summary>
        /// Registers to be notified when the volume is about to be removed
        /// This is requierd if you want to get the QUERY REMOVE messages
        /// </summary>
        /// <param name="register">true to register, false to unregister</param>
        /// <param name="fileHandle">handle of a file opened on the removable drive</param>
        private void RegisterForDeviceChange(bool register, SafeFileHandle fileHandle)
        {
            if (register)
            {
                // Register for handle
                DEV_BROADCAST_HANDLE data = new DEV_BROADCAST_HANDLE();
                data.dbch_devicetype = DBT_DEVTYP_HANDLE;
                data.dbch_reserved = 0;
                data.dbch_nameoffset = 0;
                //data.dbch_data = null;
                //data.dbch_eventguid = 0;
                data.dbch_handle = fileHandle.DangerousGetHandle(); //Marshal. fileHandle; 
                data.dbch_hdevnotify = (IntPtr)0;
                int size = Marshal.SizeOf(data);
                data.dbch_size = size;
                IntPtr buffer = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(data, buffer, true);

                mDeviceNotifyHandle = Native.RegisterDeviceNotification(mRecipientHandle, buffer, 0);
            }
            else
            {
                // close the directory handle
                if (mDirHandle != IntPtr.Zero)
                {
                    Native.CloseDirectoryHandle(mDirHandle);
                    //    string er = Marshal.GetLastWin32Error().ToString();
                }

                // unregister
                if (mDeviceNotifyHandle != IntPtr.Zero)
                {
                    Native.UnregisterDeviceNotification(mDeviceNotifyHandle);                                        
                }
                

                mDeviceNotifyHandle = IntPtr.Zero;
                mDirHandle = IntPtr.Zero;
               
                mCurrentDrive = "";
                if (mFileOnFlash != null)
                {
                    mFileOnFlash.Close();
                    mFileOnFlash = null;
                }
            }

        }

        /// <summary>
        /// Gets drive letter from a bit mask where bit 0 = A, bit 1 = B etc.
        /// There can actually be more than one drive in the mask but we 
        /// just use the last one in this case.
        /// </summary>
        /// <param name="mask"></param>
        /// <returns></returns>
        private static char DriveMaskToLetter(int mask)
        {
            char letter;
            string drives = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            // 1 = A
            // 2 = B
            // 4 = C...
            int cnt = 0;
            int pom = mask / 2;     
            while (pom != 0)
            {
                // while there is any bit set in the mask
                // shift it to the righ...                
                pom = pom / 2;
                cnt++;
            }

            if (cnt < drives.Length)
                letter = drives[cnt];
            else
                letter = '?';

            return letter;
        }

        /* 28.10.2007 - no longer needed
        /// <summary>
        /// Searches for any file in a given path and returns its full path
        /// </summary>
        /// <param name="drive">drive to search</param>
        /// <returns>path of the file or empty string</returns>
        private string GetAnyFile(string drive)
        {
            string file = "";
            // First try files in the root
            string[] files = Directory.GetFiles(drive);
            if (files.Length == 0)
            {
                // if no file in the root, search whole drive
                files = Directory.GetFiles(drive, "*.*", SearchOption.AllDirectories);
            }
                
            if (files.Length > 0)
                file = files[0];        // get the first file

            // return empty string if no file found
            return file;
        }*/
        #endregion


        #region Native Win32 API
        /// <summary>
        /// WinAPI functions
        /// </summary>        
        private class Native
        {
            //   HDEVNOTIFY RegisterDeviceNotification(HANDLE hRecipient,LPVOID NotificationFilter,DWORD Flags);
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, uint Flags);

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern uint UnregisterDeviceNotification(IntPtr hHandle);

            //
            // CreateFile  - MSDN
            const uint GENERIC_READ = 0x80000000;
            const uint OPEN_EXISTING = 3;
            const uint FILE_SHARE_READ = 0x00000001;
            const uint FILE_SHARE_WRITE = 0x00000002;
            const uint FILE_ATTRIBUTE_NORMAL = 128;
            const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
            static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);


            // should be "static extern unsafe"
            [DllImport("kernel32", SetLastError = true)]
            static extern IntPtr CreateFile(
                  string FileName,                    // file name
                  uint DesiredAccess,                 // access mode
                  uint ShareMode,                     // share mode
                  uint SecurityAttributes,            // Security Attributes
                  uint CreationDisposition,           // how to create
                  uint FlagsAndAttributes,            // file attributes
                  int hTemplateFile                   // handle to template file
                  );


            [DllImport("kernel32", SetLastError = true)]
            static extern bool CloseHandle(
                  IntPtr hObject   // handle to object
                  );

            /// <summary>
            /// Opens a directory, returns it's handle or zero.
            /// </summary>
            /// <param name="dirPath">path to the directory, e.g. "C:\\dir"</param>
            /// <returns>handle to the directory. Close it with CloseHandle().</returns>
            static public IntPtr OpenDirectory(string dirPath)
            {
                // open the existing file for reading          
                IntPtr handle = CreateFile(
                      dirPath,
                      GENERIC_READ,
                      FILE_SHARE_READ | FILE_SHARE_WRITE,
                      0,
                      OPEN_EXISTING,
                      FILE_FLAG_BACKUP_SEMANTICS | FILE_ATTRIBUTE_NORMAL,
                      0);

                if ( handle == INVALID_HANDLE_VALUE)
                    return IntPtr.Zero;
                else
                    return handle;
            }


            public static bool CloseDirectoryHandle(IntPtr handle)
            {
                return CloseHandle(handle);
            }
        }

        
        // Structure with information for RegisterDeviceNotification.
        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_HANDLE
        {
            public int dbch_size;
            public int dbch_devicetype;
            public int dbch_reserved;
            public IntPtr dbch_handle;
            public IntPtr dbch_hdevnotify;
            public Guid dbch_eventguid;
            public long dbch_nameoffset;
            //public byte[] dbch_data[1]; // = new byte[1];
            public byte dbch_data;
            public byte dbch_data1; 
        }

        // Struct for parameters of the WM_DEVICECHANGE message
        [StructLayout(LayoutKind.Sequential)]
        public struct DEV_BROADCAST_VOLUME
        {
            public int dbcv_size;
            public int dbcv_devicetype;
            public int dbcv_reserved;
            public int dbcv_unitmask;
        }
        #endregion

    }
}
