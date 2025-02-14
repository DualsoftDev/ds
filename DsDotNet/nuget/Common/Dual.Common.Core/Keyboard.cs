using System.Runtime.InteropServices;

namespace Dual.Common.Core
{
    /// <summary>
    /// Keyboard press 상태를 얻기 위한 class.
    /// <para/> - 대부분의 경우, Control.ModifierKeys 를 사용해도 되지만,
    ///         Form 이 아닌 경우나, application 시작시 shift 누름 상태 감지 등에서는
    ///         Control.ModifierKeys 가 작동하지 않으므로 low level keyboard API 를 이용한다.
    /// <para/> - http://bytes.com/topic/c-sharp/answers/448485-application-starting-detecting-shift-key-pressed
    /// <para/> - http://pinvoke.net/default.aspx/user32.GetKeyState
    /// </summary>
    public class Keyboard
    {
        /// Since SHORT is signed, high-order bit equals sign bit.
        /// Therefore to test if a given key is pressed, simply test if the value returned by GetKeyState() is negative:
        public static bool IsKeyPressed(VirtualKeyStates nVirtKey) => GetKeyState(nVirtKey) < 0;

        public static bool IsShiftKeyPressed => Keyboard.IsKeyPressed(VirtualKeyStates.VK_SHIFT);
        public static bool IsControlKeyPressed => Keyboard.IsKeyPressed(VirtualKeyStates.VK_CONTROL);
        public static bool IsAltKeyPressed => Keyboard.IsKeyPressed(VirtualKeyStates.VK_MENU);

        /// Return value
        /// * 0x0000 : 이전에 누른 적이 없고 호출 시점에도 눌려있지 않은 상태
        /// * 0x0001 : 이전에 누른 적이 있고 호출 시점에는 눌려있지 않은 상태
        /// * 0x8000 : 이전에 누른 적이 없고 호출 시점에는 눌려있는 상태
        /// * 0x8001 : 이전에 누른 적이 있고 호출 시점에도 눌려있는 상태
        [DllImport("user32.dll")]
        public static extern short GetKeyState(VirtualKeyStates nVirtKey);
    }

    public enum VirtualKeyStates : int
    {
        VK_LBUTTON = 0x01,  // Mouse L button
        VK_RBUTTON = 0x02,  // Mouse R button
        VK_CANCEL = 0x03,   // CTRL + BREAK
        VK_MBUTTON = 0x04,  // Mouse M button
        //
        VK_XBUTTON1 = 0x05, // X1 mouse button
        VK_XBUTTON2 = 0x06, // X2 mouse button
        //
        VK_BACK = 0x08,     // Backspace
        VK_TAB = 0x09,
        //
        VK_CLEAR = 0x0C,
        VK_RETURN = 0x0D,   // Enter key
        //
        VK_SHIFT = 0x10,
        VK_CONTROL = 0x11,
        VK_MENU = 0x12,     // ALT key
        VK_PAUSE = 0x13,
        VK_CAPITAL = 0x14,
        //
        VK_KANA = 0x15,
        VK_HANGEUL = 0x15,  /* old name - should be here for compatibility */
        VK_HANGUL = 0x15,
        VK_JUNJA = 0x17,
        VK_FINAL = 0x18,
        VK_HANJA = 0x19,
        VK_KANJI = 0x19,
        //
        VK_ESCAPE = 0x1B,
        //
        VK_CONVERT = 0x1C,
        VK_NONCONVERT = 0x1D,
        VK_ACCEPT = 0x1E,
        VK_MODECHANGE = 0x1F,
        //
        VK_SPACE = 0x20,
        VK_PRIOR = 0x21,        // Page up
        VK_NEXT = 0x22,         // Page down
        VK_END = 0x23,
        VK_HOME = 0x24,
        VK_LEFT = 0x25,         // arrow left
        VK_UP = 0x26,
        VK_RIGHT = 0x27,
        VK_DOWN = 0x28,
        VK_SELECT = 0x29,
        VK_PRINT = 0x2A,
        VK_EXECUTE = 0x2B,
        VK_SNAPSHOT = 0x2C,     // PrintScreen
        VK_INSERT = 0x2D,
        VK_DELETE = 0x2E,
        VK_HELP = 0x2F,
        //
        VK_NUM0 = 0x30,
        VK_NUM1 = 0x31,
        VK_NUM2 = 0x32,
        VK_NUM3 = 0x33,
        VK_NUM4 = 0x34,
        VK_NUM5 = 0x35,
        VK_NUM6 = 0x36,
        VK_NUM7 = 0x37,
        VK_NUM8 = 0x38,
        VK_NUM9 = 0x39,
        //
        VK_ALPHA_A = 0x41,      // 'A'
        VK_ALPHA_B = 0x42,      // 'B'
        VK_ALPHA_C = 0x43,      // 'C'
        VK_ALPHA_D = 0x44,      // 'D'
        VK_ALPHA_E = 0x45,      // 'E'
        VK_ALPHA_F = 0x46,      // 'F'
        VK_ALPHA_G = 0x47,      // 'G'
        VK_ALPHA_H = 0x48,      // 'H'
        VK_ALPHA_I = 0x49,      // 'I'
        VK_ALPHA_J = 0x4A,      // 'J'
        VK_ALPHA_K = 0x4B,      // 'K'
        VK_ALPHA_L = 0x4C,      // 'L'
        VK_ALPHA_M = 0x4D,      // 'M'
        VK_ALPHA_N = 0x4E,      // 'N'
        VK_ALPHA_O = 0x4F,      // 'O'
        VK_ALPHA_P = 0x50,      // 'P'
        VK_ALPHA_Q = 0x51,      // 'Q'
        VK_ALPHA_R = 0x52,      // 'R'
        VK_ALPHA_S = 0x53,      // 'S'
        VK_ALPHA_T = 0x54,      // 'T'
        VK_ALPHA_U = 0x55,      // 'U'
        VK_ALPHA_V = 0x56,      // 'V'
        VK_ALPHA_W = 0x57,      // 'W'
        VK_ALPHA_X = 0x58,      // 'X'
        VK_ALPHA_Y = 0x59,      // 'Y'
        VK_ALPHA_Z = 0x5A,      // 'Z'
        //
        VK_LWIN = 0x5B,         // Left window key
        VK_RWIN = 0x5C,         // Right window key
        VK_APPS = 0x5D,
        //
        VK_SLEEP = 0x5F,        // computer sleep key
        //
        VK_NUMPAD0 = 0x60,
        VK_NUMPAD1 = 0x61,
        VK_NUMPAD2 = 0x62,
        VK_NUMPAD3 = 0x63,
        VK_NUMPAD4 = 0x64,
        VK_NUMPAD5 = 0x65,
        VK_NUMPAD6 = 0x66,
        VK_NUMPAD7 = 0x67,
        VK_NUMPAD8 = 0x68,
        VK_NUMPAD9 = 0x69,
        VK_MULTIPLY = 0x6A,
        VK_ADD = 0x6B,          // numeric keypad +
        VK_SEPARATOR = 0x6C,
        VK_SUBTRACT = 0x6D,     // numeric keypad -
        VK_DECIMAL = 0x6E,      // numeric keypad .
        VK_DIVIDE = 0x6F,       // numeric keypad /
        VK_F1 = 0x70,
        VK_F2 = 0x71,
        VK_F3 = 0x72,
        VK_F4 = 0x73,
        VK_F5 = 0x74,
        VK_F6 = 0x75,
        VK_F7 = 0x76,
        VK_F8 = 0x77,
        VK_F9 = 0x78,
        VK_F10 = 0x79,
        VK_F11 = 0x7A,
        VK_F12 = 0x7B,
        VK_F13 = 0x7C,
        VK_F14 = 0x7D,
        VK_F15 = 0x7E,
        VK_F16 = 0x7F,
        VK_F17 = 0x80,
        VK_F18 = 0x81,
        VK_F19 = 0x82,
        VK_F20 = 0x83,
        VK_F21 = 0x84,
        VK_F22 = 0x85,
        VK_F23 = 0x86,
        VK_F24 = 0x87,
        //
        VK_NUMLOCK = 0x90,
        VK_SCROLL = 0x91,           // Scroll lock
        //
        VK_OEM_NEC_EQUAL = 0x92,   // '=' key on numpad
        //
        VK_OEM_FJ_JISHO = 0x92,   // 'Dictionary' key
        VK_OEM_FJ_MASSHOU = 0x93,   // 'Unregister word' key
        VK_OEM_FJ_TOUROKU = 0x94,   // 'Register word' key
        VK_OEM_FJ_LOYA = 0x95,   // 'Left OYAYUBI' key
        VK_OEM_FJ_ROYA = 0x96,   // 'Right OYAYUBI' key
        //
        VK_LSHIFT = 0xA0,           // left shift
        VK_RSHIFT = 0xA1,           // right shift
        VK_LCONTROL = 0xA2,         // left control
        VK_RCONTROL = 0xA3,         // right control
        VK_LMENU = 0xA4,            // left ALT
        VK_RMENU = 0xA5,            // right ALT
        //
        VK_BROWSER_BACK = 0xA6,
        VK_BROWSER_FORWARD = 0xA7,
        VK_BROWSER_REFRESH = 0xA8,
        VK_BROWSER_STOP = 0xA9,
        VK_BROWSER_SEARCH = 0xAA,
        VK_BROWSER_FAVORITES = 0xAB,
        VK_BROWSER_HOME = 0xAC,
        //
        VK_VOLUME_MUTE = 0xAD,
        VK_VOLUME_DOWN = 0xAE,
        VK_VOLUME_UP = 0xAF,
        VK_MEDIA_NEXT_TRACK = 0xB0,
        VK_MEDIA_PREV_TRACK = 0xB1,
        VK_MEDIA_STOP = 0xB2,
        VK_MEDIA_PLAY_PAUSE = 0xB3,
        VK_LAUNCH_MAIL = 0xB4,
        VK_LAUNCH_MEDIA_SELECT = 0xB5,
        VK_LAUNCH_APP1 = 0xB6,
        VK_LAUNCH_APP2 = 0xB7,
        //
        VK_OEM_1 = 0xBA,   // ';:' for US
        VK_OEM_PLUS = 0xBB,   // '+' any country
        VK_OEM_COMMA = 0xBC,   // ',' any country
        VK_OEM_MINUS = 0xBD,   // '-' any country
        VK_OEM_PERIOD = 0xBE,   // '.' any country
        VK_OEM_2 = 0xBF,   // '/?' for US
        VK_OEM_3 = 0xC0,   // '`~' for US
        //
        VK_OEM_4 = 0xDB,  //  '[{' for US
        VK_OEM_5 = 0xDC,  //  '\|' for US
        VK_OEM_6 = 0xDD,  //  ']}' for US
        VK_OEM_7 = 0xDE,  //  ''"' for US
        VK_OEM_8 = 0xDF,
        //
        VK_OEM_AX = 0xE1,  //  'AX' key on Japanese AX kbd
        VK_OEM_102 = 0xE2,  //  "<>" or "\|" on RT 102-key kbd.
        VK_ICO_HELP = 0xE3,  //  Help key on ICO
        VK_ICO_00 = 0xE4,  //  00 key on ICO
        //
        VK_PROCESSKEY = 0xE5,
        //
        VK_ICO_CLEAR = 0xE6,
        //
        VK_PACKET = 0xE7,
        //
        VK_OEM_RESET = 0xE9,
        VK_OEM_JUMP = 0xEA,
        VK_OEM_PA1 = 0xEB,
        VK_OEM_PA2 = 0xEC,
        VK_OEM_PA3 = 0xED,
        VK_OEM_WSCTRL = 0xEE,
        VK_OEM_CUSEL = 0xEF,
        VK_OEM_ATTN = 0xF0,
        VK_OEM_FINISH = 0xF1,
        VK_OEM_COPY = 0xF2,
        VK_OEM_AUTO = 0xF3,
        VK_OEM_ENLW = 0xF4,
        VK_OEM_BACKTAB = 0xF5,
        //
        VK_ATTN = 0xF6,
        VK_CRSEL = 0xF7,
        VK_EXSEL = 0xF8,
        VK_EREOF = 0xF9,
        VK_PLAY = 0xFA,
        VK_ZOOM = 0xFB,
        VK_NONAME = 0xFC,
        VK_PA1 = 0xFD,
        VK_OEM_CLEAR = 0xFE
    }
}
