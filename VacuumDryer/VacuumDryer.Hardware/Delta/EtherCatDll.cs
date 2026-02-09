using System.Runtime.InteropServices;

namespace VacuumDryer.Hardware.Delta;

/// <summary>
/// 台達 EtherCAT DLL P/Invoke 封裝
/// 控制卡: PCI-L221B1D0
/// </summary>
public static class EtherCatDll
{
    private const string DLL_NAME = "MasterEcat.dll";

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_Init(int CardId);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_Close(int CardId);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_GetCardCount();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_ServoOn(int CardId, int AxisId);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_ServoOff(int CardId, int AxisId);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_Home(int CardId, int AxisId, int HomeMode);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_WaitHomeDone(int CardId, int AxisId, int TimeoutMs);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_AbsMove(int CardId, int AxisId, double Position, double Velocity, double Acc, double Dec);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_RelMove(int CardId, int AxisId, double Distance, double Velocity, double Acc, double Dec);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_JogMove(int CardId, int AxisId, double Velocity, double Acc, double Dec);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_Stop(int CardId, int AxisId, double Dec);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_EmgStop(int CardId, int AxisId);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_WaitMoveDone(int CardId, int AxisId, int TimeoutMs);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_GetActualPos(int CardId, int AxisId, out double Position);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_GetActualVel(int CardId, int AxisId, out double Velocity);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_GetActualTorque(int CardId, int AxisId, out double Torque);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_GetAxisStatus(int CardId, int AxisId, out uint Status);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_IsMoving(int CardId, int AxisId, out int IsMoving);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_IsServoOn(int CardId, int AxisId, out int IsOn);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_ReadDI(int CardId, int Channel, out int Value);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_WriteDO(int CardId, int Channel, int Value);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_ReadDO(int CardId, int Channel, out int Value);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_ClearAlarm(int CardId, int AxisId);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int Ecat_GetErrorCode(int CardId, int AxisId, out uint ErrorCode);
}

public static class EtherCatError
{
    public const int SUCCESS = 0;
    public const int ERR_CARD_NOT_FOUND = -1;
    public const int ERR_AXIS_NOT_FOUND = -2;
    public const int ERR_NOT_INITIALIZED = -3;
    public const int ERR_SERVO_NOT_ON = -4;
    public const int ERR_MOVING = -5;
    public const int ERR_TIMEOUT = -6;
    public const int ERR_ALARM = -7;
}
