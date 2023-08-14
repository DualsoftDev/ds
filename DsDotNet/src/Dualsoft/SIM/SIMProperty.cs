using System;

namespace DSModeler
{
    public static class SIMProperty
    {
        static readonly int _defaultSpeed = 3;
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


