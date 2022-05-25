import ctypes
import time

paix = ctypes.CDLL(r'./Dll/x64/NMC2.dll')

N = 64
Output = (ctypes.c_short*N)()
Input = (ctypes.c_short*N)()

ip = 12;       # for 192.168.0.12
r1 = paix.nmc_OpenDevice(ip)

nRet = paix.nmc_GetDIOOutput(ip, Output)

# test 살릴 출력 LED : 2, 3, 4, 5
nRet = paix.nmc_SetDIOOutputBit(ip, 2, 1)

for i in range(N):
    if Output[i] :
        print(i, "=", Output[i])

# nRet2 = paix.nmc_GetDIOInput(ip, Input)
# for i in range(N):
#     print(Input[i])
        
nRet = paix.nmc_SetDIOOutputBit(ip, 7, 1)
time.sleep(2.0)
nRet = paix.nmc_SetDIOOutputBit(ip, 7, 0)
# nRet = paix.nmc_GetDIOOutput(ip, Output)

# if( nRet != NMC_OK )	return
# nRet = paix.nmc_GetDIOInput(ip, Input)
# if( nRet != NMC_OK )	return

# 	nRet = nmc_SetDIOOutputTog(ip,nBitno)
# 	nRet = nmc_SetDIOOutput(ip,Output)
# 	nRet = nmc_SetDIOOutputBit(ip,nBitno,1)
# 		nmc_CloseDevice(ip)


# xxx = paix.NMC_OK

#paix.nmc_PingCheck(deviceNumber, defaultArg waitTimeMilli timeout) = paix.NMC_OK

# r2 = paix.nmc_OpenDeviceEx(0)

print("paix")
