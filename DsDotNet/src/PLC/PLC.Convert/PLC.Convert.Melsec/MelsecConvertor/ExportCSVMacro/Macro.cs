using Dsu.Common.CS.LSIS.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MelsecConverter
{
    public class KeyboardHelper
    {
        // Context Menu (Applications) 키의 가상 키 코드
        private const byte VK_APPS = 0x5D;

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public static void PressContextMenuKey()
        {
            keybd_event(VK_APPS, 0, 0, UIntPtr.Zero); // 키 누름
            keybd_event(VK_APPS, 0, 2, UIntPtr.Zero); // 키 뗌
        }

        public static bool IsKeyPressed(int vKey)
        {
            return (GetAsyncKeyState(vKey) & 0x8000) != 0;
        }
    }

    class Macro
    {
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static CancellationToken? _token => _cancellationTokenSource.Token;
        private static bool _isMacroRunning = false;
        private static bool _isMacroESCPush = false;
        public static int Delay = 2000;

        #region 외부에서 사용하는 public 함수
        public static bool StartExportGx3()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _isMacroRunning = true;

            // ESC 키 감지 작업 실행
            _ = Task.Run(async () => await MonitorEscKey());
            // 비동기 작업 시작
            return doExportGx3(_cancellationTokenSource.Token);
        }
        public static bool StartExportGx2(int count)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _isMacroRunning = true;

            // ESC 키 감지 작업 실행
            _ = Task.Run(async () => await MonitorEscKey());
            // 비동기 작업 시작
            return doExportGx2(count, _cancellationTokenSource.Token);
        }
        private static async Task MonitorEscKey()
        {
            while (_isMacroRunning)
            {
                if (!_isMacroESCPush && KeyboardHelper.IsKeyPressed(0x1B)) // ESC 키
                {
                    Console.WriteLine("ESC key pressed. Stopping macro...");
                    StopExport();
                    _isMacroRunning = false;
                }
                await Task.Delay(100); // CPU 사용량을 줄이기 위한 대기
            }
        }

        public static void StopExport()
        {
            // 이벤트로 중지 요청을 발생시키기 위해 별도의 스레드에서 작업 시작
            var res = Task.Run(() => triggerStopExport());
        }
        #endregion 외부에서 사용하는 public 함수

        static void triggerStopExport()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch(ObjectDisposedException)
            {
                Console.WriteLine("cancellationTokenSource is disposed");
            }
        }
        static bool doExportGx2(int count, CancellationToken cancellationToken)
        {
            try
            {
                wait(5000);
                for (int i = 0; i < count; i++)
                {
                    gx2FocusNavigation();
                    if (i == 0)
                        home();
                    else
                    {
                        moveDown();
                        wait(50);
                    }
                    openMenu();
                    sendAndWaitShort("o");
                    yesAndSave(); wait(50);
                    yesAndSave(); wait(50);
                    yesAndSave(); wait(50);
                    yesAndSave(); wait(50);
                    esc();
                    esc();
                    wait(Delay);     //저장 딜레이 2초
                }
                return true;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("작업이 취소되었습니다.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                return false;

            }
            finally
            {
                _cancellationTokenSource.Dispose();
            }
        }
        private static void filterProgram()
        {       
            openQukSearch();
            wait(1000);
            programBody();
            enter();
            for (int i = 0; i < 7; i++)
            {
                wait(100);
                moveLeft();
            }
            for (int i = 0; i < 3; i++)
            {
                wait(50);
                moveRight();
            }

            sendAndWaitShort("p");
            sendAndWaitShort("p");
            enter();
        }

        static bool doExportGx3(CancellationToken cancellationToken)
        {
            try
            {
                wait(5000);
                filterProgram();
                wait(2000);  //처음 로딩 시간 간헐적인 발생

                moveDown(1);
                ctrlA();
                ctrlC();

                var count = getClipboardLineCount() - 1;//Header 제외 

                for (int i = 0; i < count; i++)
                {
                    filterProgram();

                    enter();
                    wait(500);
                    moveDown(i);
                    moveDown();
                    openMenu();
                    key("d");
                    wait(500);
                    openMenu();
                    key("j");
                    wait(200);
                    leftAndEnter();
                    wait(200);
                    leftAndEnter();
                    wait(200);
                    leftAndEnter();
                    wait(Delay);     //programbody 저장시간 2초
                    esc();
                    wait(200);
                    esc();
                    wait(200);
                    closeOne();
                    wait(100);
                }
                
                //exportGlobal();
                exportCommonDeviceComment();

                return true;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("작업이 취소되었습니다.");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                return false;
            }
            finally
            {
                _cancellationTokenSource.Dispose();
            }
        }
        static void exportGlobal()
        {
            wait(1000);
            openQukSearch();
            PasteText("Global");
            enter();
            wait(2000);  //처음 로딩 시간 간헐적인 발생
            moveDown();
            openMenu();
            key("d");
            wait(500);
            openMenu();
            key("o");
            wait(200);
            leftAndEnter();
            wait(200);
            yesAndSave();
            wait(200);
            yesAndSave();
            wait(5000);        //저장시간 5초
            esc();
            wait(200);
            esc();
            wait(200);
            closeOne();
            wait(100);

        }
        static void exportCommonDeviceComment()
        {
            wait(1000);
            openQukSearch();
            PasteText("common device comment");
            enter();
            wait(2000);  //처음 로딩 시간 간헐적인 발생
            moveDown();
            openMenu();
            key("d");
            wait(500);

            tab();
            tab();

            openMenu();
            key("o");
            wait(200);
            yesAndSave();
            wait(200);
            yesAndSave();
            wait(200);
            yesAndSave();
            wait(15000);        // commonDeviceComment 저장시간 15초
            esc();
            wait(200);
            esc();
            wait(200);
            closeOne();
            wait(100);
        }

#region MacroFunctions
        static void sendAndWaitShort(string str)
        {
            _token?.ThrowIfCancellationRequested();
            //Console.WriteLine($"key pressed : {str}");
            SendKeys.SendWait(str);
            wait(50);
        }
        static void wait(int mSec)
        {
            _token?.ThrowIfCancellationRequested();
            Task.Delay(mSec).Wait();
        }
        static void key(string str) => sendAndWaitShort(str);
        static void programBody() => PasteText("PROGRAMBODY");

        static void PasteText(string str)
        {
            Thread staThread = new Thread(() =>
            {
                Clipboard.SetText(str);
                wait(50);
                // 현재 포커스가 있는 창에 붙여넣기 (Ctrl + V 시뮬레이션)
                sendAndWaitShort("^v");
            });
            staThread.SetApartmentState(ApartmentState.STA); // 스레드를 STA 모드로 설정
            staThread.Start();
            staThread.Join(); // 스레드 작업 완료 대기
        }

        static int getClipboardLineCount()
        {
            // 클립보드에서 텍스트를 가져옴
            if (Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();

                // 줄바꿈을 기준으로 나누어 배열로 만들고 개수를 반환
                string[] lines = clipboardText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                return lines.Length;
            }
            else
            {
                throw new InvalidOperationException("Clipboard에 텍스트가 없습니다.");
            }
        }

        static void openQukSearch() => sendAndWaitShort("^q");
        static void closeOne() => sendAndWaitShort("^{F4}");
        static void openMenu()
        {
            KeyboardHelper.PressContextMenuKey();
            wait(100);
        }

        static void enter() => sendAndWaitShort("{ENTER}");

        static void yes() => sendAndWaitShort("%y");
        static void save() => sendAndWaitShort("%s");

        static void yesAndSave()
        {
            yes();
            save();
        }
        static void leftAndEnter()
        {
            moveLeft();
            enter();
        }

        static void home() => sendAndWaitShort("{HOME}");
        static void esc()
        {
            _isMacroESCPush = true;
            sendAndWaitShort("{ESC}");
            _isMacroESCPush = false;
        }
        static void tab() => sendAndWaitShort("{TAB}");
        static void ctrlA() => sendAndWaitShort("^a"); // Ctrl + A
        static void ctrlC() => sendAndWaitShort("^c"); // Ctrl + C

        static void moveLeft() => sendAndWaitShort("{Left}");
        static void moveRight() => sendAndWaitShort("{Right}");
        static void moveDown() => SendKeys.SendWait("{DOWN}"); //딜레이 제거
        static void moveDown(int cnt)
        {
            for (int i = 0; i < cnt; i++)
            {
                moveDown();
                wait(50);
            }
        }





        static void gx2FocusNavigation()
        {
            sendAndWaitShort("%v");
            sendAndWaitShort("k");
            sendAndWaitShort("p");
            yes();

            sendAndWaitShort("%v");
            sendAndWaitShort("k");
            sendAndWaitShort("n");
            sendAndWaitShort("%v");
            sendAndWaitShort("k");
            sendAndWaitShort("n");
        }
        #endregion MacroFunctions

    }
}
