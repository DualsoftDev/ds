using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransparentWindow : MonoBehaviour
{
    private bool windowedTrigger = false;
    public struct MARGINS
    {
        public int leftWidth;
        public int rightWidth;
        public int topHeight;
        public int bottomHeight;
    }
 
    [DllImport("user32.dll")]
    public static extern IntPtr GetActiveWindow();
 
    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
 
    [DllImport("user32.dll")]
    public static extern int BringWindowToTop(IntPtr hwnd);

    [DllImport("user32.dll")]
    public static extern int SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    public static extern int SetForegroundWindowForce(IntPtr hwnd);
 
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
    
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
 
    [DllImport("Dwmapi.dll")]
    public static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);
 
    public static readonly System.IntPtr HWND_TOPMOST = new System.IntPtr(-1);
    //public static readonly System.IntPtr HWND_NOT_TOPMOST = new System.IntPtr(-2);
    
 
    IntPtr hWnd;
    const UInt32 SWP_NOSIZE = 0x0001;
    const UInt32 SWP_NOMOVE = 0x0002;
    const UInt32 SWP_NOZORDER  = 0x0004;
    

    const int SWP_SHOWWINDOW = 0x40;
    const int SWP_NOACTIVATE = 0x10;

    const int GWL_EXSTYLE = -20;
    const int GWL_STYLE = -16;
    const uint WS_EX_LAYERED = 0x00080000;
    const uint WS_EX_TRANSPARENT = 0x00000020;
    const uint SWP_ASYNCWINDOWPOS = 0x4000;
    const uint SWP_NOOWNERZORDER = 0x0200;

    private void OnEnable()
    {
        Screen.SetResolution(1920, 1080, true);
        Application.runInBackground = true;

        hWnd = GetActiveWindow();

        MARGINS margins = new MARGINS { leftWidth = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);
        SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED);

        BringWindowToTop(hWnd);
        //SetForegroundWindowForce(hWnd);
        //SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | WS_EX_TRANSPARENT);
        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_SHOWWINDOW | SWP_NOSIZE | WS_EX_TRANSPARENT | SWP_NOACTIVATE);
        //SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOOWNERZORDER | SWP_ASYNCWINDOWPOS|SWP_NOSIZE|SWP_SHOWWINDOW|SWP_NOMOVE | WS_EX_TRANSPARENT);
    }
 
    public void Update()    //코루틴으로 최적화?
    {
        if(Screen.fullScreen == true)
        {
            BringWindowToTop(hWnd);
            if(!windowedTrigger)
            {
                windowedTrigger = true;
                SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
                SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | WS_EX_TRANSPARENT);
            }
            
        }
        else
        { 
            if(windowedTrigger)
                {
                    SetWindowLong(hWnd, GWL_EXSTYLE,WS_EX_LAYERED);
                    MoveWindow(hWnd, Screen.width/2, Screen.height/2, 1280, 720, false);
                    windowedTrigger = false;
                }
            
        }
    }
}
 
