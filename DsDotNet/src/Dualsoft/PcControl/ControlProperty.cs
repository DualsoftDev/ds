using System;

namespace DSModeler
{
    public static class ControlProperty
    {
        static readonly int _defaultSpeed = 4;
        public static int GetDelayMsec()
        {
            int delayMsec;
            switch (Global.SimSpeed)
            {
                case 0: delayMsec = 500; ; break;
                case 1: delayMsec = 200; ; break;
                case 2: delayMsec = 100; ; break;
                case 3: delayMsec = 20; ; break;
                case 4: delayMsec = 5; ; break;
                case 5: delayMsec = 0; ; break;
                default: delayMsec = 5; ; break;
            }
            return delayMsec;
        }
        public static void SetSpeed(int speed)
        {
            Global.SimSpeed = speed;
            DSRegistry.SetValue(K.SimSpeed, Global.SimSpeed);
        }
        public static int GetSpeed()
        {
            var regSpeed = DSRegistry.GetValue(K.SimSpeed);
            if (regSpeed != null)  //초기 실행시 레지 없으면
                return Convert.ToInt32(regSpeed);
            else
            {
                SetSpeed(_defaultSpeed); //초기 실행시 default 값
                return _defaultSpeed;
            }
        }

    }
}


