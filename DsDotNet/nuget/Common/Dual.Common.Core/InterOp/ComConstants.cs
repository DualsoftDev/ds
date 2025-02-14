using System;
using System.ComponentModel;

namespace Dual.Common.Core.InterOp
{
    public sealed class ErrorHandler
    {
        public static bool Failed(int hr) { return hr < 0; }
        public static bool Succeeded(int hr) {  return hr >= 0; }

        //public static int ThrowOnFailure(int hr);
        //public static int ThrowOnFailure(int hr, params int[] expectedHRFailure);
    }

    /// <summary>
    /// Carbon copy of Microsoft.VisualStudio.VSConstants.
    /// To remove Microsoft.VisualStudio.Shell.12.0 reference
    /// </summary>
    ///
    public sealed class ComConstants
    {
        public const uint ALL = 1;
        public const string AssemblyReferenceProvider_string = "{9A341D95-5A64-11D3-BFF9-00C04F990235}";
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const uint CEF_CLONEFILE = 1;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const uint CEF_OPENASNEW = 8;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const uint CEF_OPENFILE = 2;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const uint CEF_SILENT = 4;
        public const int cmdidToolsOptions = 264;
        public const int CO_E_ACCESSCHECKFAILED = -2147417814;
        public const int CO_E_ACESINWRONGORDER = -2147417798;
        public const int CO_E_ACNOTINITIALIZED = -2147417793;
        public const int CO_E_CANCEL_DISABLED = -2147417792;
        public const int CO_E_CONVERSIONFAILED = -2147417810;
        public const int CO_E_DECODEFAILED = -2147417795;
        public const int CO_E_EXCEEDSYSACLLIMIT = -2147417799;
        public const int CO_E_FAILEDTOCLOSEHANDLE = -2147417800;
        public const int CO_E_FAILEDTOCREATEFILE = -2147417801;
        public const int CO_E_FAILEDTOGENUUID = -2147417802;
        public const int CO_E_FAILEDTOGETSECCTX = -2147417820;
        public const int CO_E_FAILEDTOGETTOKENINFO = -2147417818;
        public const int CO_E_FAILEDTOGETWINDIR = -2147417804;
        public const int CO_E_FAILEDTOIMPERSONATE = -2147417821;
        public const int CO_E_FAILEDTOOPENPROCESSTOKEN = -2147417796;
        public const int CO_E_FAILEDTOOPENTHREADTOKEN = -2147417819;
        public const int CO_E_FAILEDTOQUERYCLIENTBLANKET = -2147417816;
        public const int CO_E_FAILEDTOSETDACL = -2147417815;
        public const int CO_E_INCOMPATIBLESTREAMVERSION = -2147417797;
        public const int CO_E_INVALIDSID = -2147417811;
        public const int CO_E_LOOKUPACCNAMEFAILED = -2147417806;
        public const int CO_E_LOOKUPACCSIDFAILED = -2147417808;
        public const int CO_E_NETACCESSAPIFAILED = -2147417813;
        public const int CO_E_NOMATCHINGNAMEFOUND = -2147417807;
        public const int CO_E_NOMATCHINGSIDFOUND = -2147417809;
        public const int CO_E_PATHTOOLONG = -2147417803;
        public const int CO_E_SETSERLHNDLFAILED = -2147417805;
        public const int CO_E_TRUSTEEDOESNTMATCHCLIENT = -2147417817;
        public const int CO_E_WRONGTRUSTEENAMESYNTAX = -2147417812;
        public const string ComReferenceProvider_string = "{4560BE15-8871-482A-801D-76AA47F1763A}";
        public const string ConnectedServiceInstanceReferenceProvider_string = "{C18E5D73-E6D1-43AA-AC5E-58D82E44DA9C}";
        public const int CPDN_SELCHANGED = 2304;
        public const int CPDN_SELDBLCLICK = 2305;
        public const int CPPM_CLEARSELECTION = 2314;
        public const int CPPM_GETSELECTION = 2311;
        public const int CPPM_INITIALIZELIST = 2309;
        public const int CPPM_INITIALIZETAB = 2312;
        public const int CPPM_QUERYCANSELECT = 2310;
        public const int CPPM_SETMULTISELECT = 2313;
        public const int DISP_E_ARRAYISLOCKED = -2147352563;
        public const int DISP_E_BADCALLEE = -2147352560;
        public const int DISP_E_BADINDEX = -2147352565;
        public const int DISP_E_BADPARAMCOUNT = -2147352562;
        public const int DISP_E_BADVARTYPE = -2147352568;
        public const int DISP_E_BUFFERTOOSMALL = -2147352557;
        public const int DISP_E_DIVBYZERO = -2147352558;
        public const int DISP_E_EXCEPTION = -2147352567;
        public const int DISP_E_MEMBERNOTFOUND = -2147352573;
        public const int DISP_E_NONAMEDARGS = -2147352569;
        public const int DISP_E_NOTACOLLECTION = -2147352559;
        public const int DISP_E_OVERFLOW = -2147352566;
        public const int DISP_E_PARAMNOTFOUND = -2147352572;
        public const int DISP_E_PARAMNOTOPTIONAL = -2147352561;
        public const int DISP_E_TYPEMISMATCH = -2147352571;
        public const int DISP_E_UNKNOWNINTERFACE = -2147352575;
        public const int DISP_E_UNKNOWNLCID = -2147352564;
        public const int DISP_E_UNKNOWNNAME = -2147352570;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const uint DocumentFrame = 2;
        public const int E_ABORT = -2147467260;
        public const int E_ACCESSDENIED = -2147024891;
        public const int E_FAIL = -2147467259;
        public const int E_HANDLE = -2147024890;
        public const int E_INVALIDARG = -2147024809;
        public const int E_NOINTERFACE = -2147467262;
        public const int E_NOTIMPL = -2147467263;
        public const int E_OUTOFMEMORY = -2147024882;
        public const int E_PENDING = -2147483638;
        public const int E_POINTER = -2147467261;
        public const int E_UNEXPECTED = -2147418113;
        public const string FileReferenceProvider_string = "{7B069159-FF02-4752-93E8-96B3CADF441A}";
        public const string MiscFilesProjectUniqueName = "<MiscFiles>";
        public const int OLE_E_ADVF = -2147221503;
        public const int OLE_E_ADVISENOTSUPPORTED = -2147221501;
        public const int OLE_E_BLANK = -2147221497;
        public const int OLE_E_CANT_BINDTOSOURCE = -2147221494;
        public const int OLE_E_CANT_GETMONIKER = -2147221495;
        public const int OLE_E_CANTCONVERT = -2147221487;
        public const int OLE_E_CLASSDIFF = -2147221496;
        public const int OLE_E_ENUM_NOMORE = -2147221502;
        public const int OLE_E_INVALIDHWND = -2147221489;
        public const int OLE_E_INVALIDRECT = -2147221491;
        public const int OLE_E_NOCACHE = -2147221498;
        public const int OLE_E_NOCONNECTION = -2147221500;
        public const int OLE_E_NOSTORAGE = -2147221486;
        public const int OLE_E_NOT_INPLACEACTIVE = -2147221488;
        public const int OLE_E_NOTRUNNING = -2147221499;
        public const int OLE_E_OLEVERB = -2147221504;
        public const int OLE_E_PROMPTSAVECANCELLED = -2147221492;
        public const int OLE_E_STATIC = -2147221493;
        public const int OLE_E_WRONGCOMPOBJ = -2147221490;
        public const string PlatformReferenceProvider_string = "{97324595-E3F9-4AA8-85B7-DC941E812152}";
        public const string ProjectReferenceProvider_string = "{51ECA6BD-5AE4-43F0-AA76-DD0A7B08F40C}";
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const uint PropertyBrowserSID = 4;
        public const int RPC_E_ACCESS_DENIED = -2147417829;
        public const int RPC_E_ATTEMPTED_MULTITHREAD = -2147417854;
        public const int RPC_E_CALL_CANCELED = -2147418110;
        public const int RPC_E_CALL_COMPLETE = -2147417833;
        public const int RPC_E_CALL_REJECTED = -2147418111;
        public const int RPC_E_CANTCALLOUT_AGAIN = -2147418095;
        public const int RPC_E_CANTCALLOUT_INASYNCCALL = -2147418108;
        public const int RPC_E_CANTCALLOUT_INEXTERNALCALL = -2147418107;
        public const int RPC_E_CANTCALLOUT_ININPUTSYNCCALL = -2147417843;
        public const int RPC_E_CANTPOST_INSENDCALL = -2147418109;
        public const int RPC_E_CANTTRANSMIT_CALL = -2147418102;
        public const int RPC_E_CHANGED_MODE = -2147417850;
        public const int RPC_E_CLIENT_CANTMARSHAL_DATA = -2147418101;
        public const int RPC_E_CLIENT_CANTUNMARSHAL_DATA = -2147418100;
        public const int RPC_E_CLIENT_DIED = -2147418104;
        public const int RPC_E_CONNECTION_TERMINATED = -2147418106;
        public const int RPC_E_DISCONNECTED = -2147417848;
        public const int RPC_E_FAULT = -2147417852;
        public const int RPC_E_FULLSIC_REQUIRED = -2147417823;
        public const int RPC_E_INVALID_CALLDATA = -2147417844;
        public const int RPC_E_INVALID_DATA = -2147418097;
        public const int RPC_E_INVALID_DATAPACKET = -2147418103;
        public const int RPC_E_INVALID_EXTENSION = -2147417838;
        public const int RPC_E_INVALID_HEADER = -2147417839;
        public const int RPC_E_INVALID_IPID = -2147417837;
        public const int RPC_E_INVALID_OBJECT = -2147417836;
        public const int RPC_E_INVALID_OBJREF = -2147417827;
        public const int RPC_E_INVALID_PARAMETER = -2147418096;
        public const int RPC_E_INVALID_STD_NAME = -2147417822;
        public const int RPC_E_INVALIDMETHOD = -2147417849;
        public const int RPC_E_NO_CONTEXT = -2147417826;
        public const int RPC_E_NO_GOOD_SECURITY_PACKAGES = -2147417830;
        public const int RPC_E_NO_SYNC = -2147417824;
        public const int RPC_E_NOT_REGISTERED = -2147417853;
        public const int RPC_E_OUT_OF_RESOURCES = -2147417855;
        public const int RPC_E_REMOTE_DISABLED = -2147417828;
        public const int RPC_E_RETRY = -2147417847;
        public const int RPC_E_SERVER_CANTMARSHAL_DATA = -2147418099;
        public const int RPC_E_SERVER_CANTUNMARSHAL_DATA = -2147418098;
        public const int RPC_E_SERVER_DIED = -2147418105;
        public const int RPC_E_SERVER_DIED_DNE = -2147418094;
        public const int RPC_E_SERVERCALL_REJECTED = -2147417845;
        public const int RPC_E_SERVERCALL_RETRYLATER = -2147417846;
        public const int RPC_E_SERVERFAULT = -2147417851;
        public const int RPC_E_SYS_CALL_FAILED = -2147417856;
        public const int RPC_E_THREAD_NOT_INIT = -2147417841;
        public const int RPC_E_TIMEOUT = -2147417825;
        public const int RPC_E_TOO_LATE = -2147417831;
        public const int RPC_E_UNEXPECTED = -2147352577;
        public const int RPC_E_UNSECURE_CALL = -2147417832;
        public const int RPC_E_VERSION_MISMATCH = -2147417840;
        public const int RPC_E_WRONG_THREAD = -2147417842;
        public const int RPC_S_CALLPENDING = -2147417835;
        public const int RPC_S_WAITONTIMER = -2147417834;
        public const int S_FALSE = 1;
        public const int S_OK = 0;
        public const uint SELECTED = 2;
        public const string SolutionItemsProjectUniqueName = "<SolnItems>";
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const uint StartupProject = 3;
        public const int UNDO_E_CLIENTABORT = -2147205119;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const uint UndoManager = 0;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const uint UserContext = 5;
        public const uint VS_BUILDABLEPROJECTCFGOPTS_BUILD_ACTIVE_DOCUMENT_ONLY = 4;
        public const uint VS_BUILDABLEPROJECTCFGOPTS_BUILD_SELECTION_ONLY = 2;
        public const uint VS_BUILDABLEPROJECTCFGOPTS_PACKAGE = 8;
        public const uint VS_BUILDABLEPROJECTCFGOPTS_PRIVATE = 4294901760;
        public const uint VS_BUILDABLEPROJECTCFGOPTS_REBUILD = 1;
        public const int VS_E_BUSY = -2147220992;
        public const int VS_E_CIRCULARTASKDEPENDENCY = -2147213305;
        public const int VS_E_INCOMPATIBLECLASSICPROJECT = -2147213308;
        public const int VS_E_INCOMPATIBLEDOCDATA = -2147213334;
        public const int VS_E_INCOMPATIBLEPROJECT = -2147213309;
        public const int VS_E_INCOMPATIBLEPROJECT_UNSUPPORTED_OS = -2147213307;
        public const int VS_E_PACKAGENOTLOADED = -2147213343;
        public const int VS_E_PROJECTALREADYEXISTS = -2147213344;
        public const int VS_E_PROJECTMIGRATIONFAILED = -2147213339;
        public const int VS_E_PROJECTNOTLOADED = -2147213342;
        public const int VS_E_PROMPTREQUIRED = -2147213306;
        public const int VS_E_SOLUTIONALREADYOPEN = -2147213340;
        public const int VS_E_SOLUTIONNOTOPEN = -2147213341;
        public const int VS_E_SPECIFYING_OUTPUT_UNSUPPORTED = -2147220991;
        public const int VS_E_UNSUPPORTEDFORMAT = -2147213333;
        public const int VS_E_WIZARDBACKBUTTONPRESS = -2147213313;
        public const int VS_S_INCOMPATIBLEPROJECT = 270325;
        public const int VS_S_PROJECT_ONEWAYUPGRADEREQUIRED = 270324;
        public const int VS_S_PROJECT_SAFEREPAIRREQUIRED = 270322;
        public const int VS_S_PROJECT_UNSAFEREPAIRREQUIRED = 270323;
        public const int VS_S_PROJECTFORWARDED = 270320;
        public const int VS_S_TBXMARKER = 270321;
        public const uint VSCOOKIE_NIL = 0;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const uint VSITEMID_NIL = 4294967295;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const uint VSITEMID_ROOT = 4294967294;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const uint VSITEMID_SELECTION = 4294967293;
        public const int VSM_ENTERMODAL = 4179;
        public const int VSM_EXITMODAL = 4180;
        public const int VSM_TOOLBARMETRICSCHANGE = 4178;
        public const uint VSUTDCF_DTEEONLY = 1;
        public const uint VSUTDCF_PACKAGE = 4;
        public const uint VSUTDCF_PRIVATE = 4294901760;
        public const uint VSUTDCF_REBUILD = 2;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public const uint WindowFrame = 1;

        public static readonly Guid AssemblyReferenceProvider_Guid;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid BuildOrder;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid BuildOutput;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid CLSID_ComPlusOnlyDebugEngine;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid CLSID_HtmDocData;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid CLSID_HtmedPackage;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid CLSID_HtmlLanguageService;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid CLSID_MiscellaneousFilesProject;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid CLSID_SolutionItemsProject;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid CLSID_VsCfgProviderEventsHelper;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid CLSID_VsDocOutlinePackage;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid CLSID_VsEnvironmentPackage;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid CLSID_VsTaskList;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid CLSID_VsTaskListPackage;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid CLSID_VsUIHierarchyWindow;
        public static readonly Guid ComReferenceProvider_Guid;
        public static readonly Guid ConnectedServiceInstanceReferenceProvider_Guid;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid DebugOutput;
        public static readonly Guid FileReferenceProvider_Guid;
        public static readonly Guid GUID_AppCommand;
        public static readonly Guid GUID_BrowseFilePage;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_BuildOutputWindowPane;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_COMClassicPage;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_COMPlusPage;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_DefaultEditor;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_ExternalEditor;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_HTMEDAllowExistingDocData;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_HtmlEditorFactory;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_ItemType_PhysicalFile;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_ItemType_PhysicalFolder;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_ItemType_SubProject;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_ItemType_VirtualFolder;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_OutWindowDebugPane;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_OutWindowGeneralPane;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_ProjectDesignerEditor;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_SolutionPage;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_TextEditorFactory;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_VS_DEPTYPE_BUILD_PROJECT;
        public static readonly Guid GUID_VsNewProjectPseudoFolder;
        public static readonly Guid GUID_VSStandardCommandSet97;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_VsTaskListViewAll;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_VsTaskListViewCheckedTasks;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_VsTaskListViewCommentTasks;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_VsTaskListViewCompilerTasks;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_VsTaskListViewCurrentFileTasks;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_VsTaskListViewHTMLTasks;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_VsTaskListViewShortcutTasks;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_VsTaskListViewUncheckedTasks;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_VsTaskListViewUserTasks;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid GUID_VsUIHierarchyWindowCmds;
        public static readonly Guid guidCOMPLUSLibrary;
        public static readonly IntPtr HIERARCHY_DONTCHANGE;
        public static readonly IntPtr HIERARCHY_DONTPROPAGATE;
        public static readonly Guid IID_IUnknown;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid LOGVIEWID_Any;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid LOGVIEWID_Code;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid LOGVIEWID_Debugging;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid LOGVIEWID_Designer;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid LOGVIEWID_Primary;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid LOGVIEWID_TextView;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid LOGVIEWID_UserChooseView;
        public static readonly Guid PlatformReferenceProvider_Guid;
        public static readonly Guid ProjectReferenceProvider_Guid;
        public static readonly IntPtr SELCONTAINER_DONTCHANGE;
        public static readonly IntPtr SELCONTAINER_DONTPROPAGATE;
        public static readonly Guid SID_SUIHostCommandDispatcher;
        public static readonly Guid SID_SVsGeneralOutputWindowPane;
        public static readonly Guid SID_SVsToolboxActiveXDataProvider;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid UICONTEXT_CodeWindow;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid UICONTEXT_Debugging;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid UICONTEXT_DesignMode;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid UICONTEXT_Dragging;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid UICONTEXT_EmptySolution;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid UICONTEXT_FullScreenMode;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid UICONTEXT_NoSolution;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid UICONTEXT_SolutionBuilding;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid UICONTEXT_SolutionExists;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid UICONTEXT_SolutionHasAppContainerProject;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid UICONTEXT_SolutionHasMultipleProjects;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid UICONTEXT_SolutionHasSingleProject;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid VsStd11;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid VsStd12;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid VsStd2010;
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly Guid VSStd2K;
    }
}
