- IOHub 의 data 저장 기준은 intel architecture 기준의 little endian 이다.
- Big endian system 의 고려 사항
	- e.g ARM CPU architecture 등에서 client 를 작성할 때
	- [[Client]] 에서 [[Server]]로 data 를 전송할 때에 int, word, double word, long word 등에 대해서 little [[Endian]] 을 적용하여 bytes array 로 변환하여야 전송하여야 한다.
	- Server 에서 받은 data 를 해석할 때도 little endian 으로 전송되므로 big endian 으로 변환하여 해석하여야 한다.
- Intel CPU 기반은 little endian 이므로 byte 의 순서를 뒤집을 필요가 없이 그냥 사용
	- Dotnet 의 경우, BitConverter 등을 이용하면 됨.

- 파일에 바이트 순서가 b0, b1, b2, b3으로 저장되어 있을 때, 이를 `int` 값으로 읽는 경우는 시스템의 endian 방식에 따라 다음과 같이 다르게 해석됩니다.   파일에서 읽은 바이트를 b0이 가장 낮은 바이트(0번지), b3이 가장 높은 바이트라고 가정할 때:

- **Little Endian 시스템에서**:
    
    - 파일에서 바이트를 순서대로 읽으면, 실제 메모리에는 b0, b1, b2, b3 순서로 저장됩니다.
    - `int` 값으로 해석할 때: b0가 LSB(Least Significant Byte, 가장 작은 가중치를 가진 바이트)가 되고, b3가 MSB(Most Significant Byte, 가장 큰 가중치를 가진 바이트)가 됩니다.
    - `int` 값은 `(b3 << 24) | (b2 << 16) | (b1 << 8) | b0`의 형태로 표현됩니다.
- **Big Endian 시스템에서**:
    
    - 파일에서 바이트를 순서대로 읽으면, 실제 메모리에는 b3, b2, b1, b0 순서로 저장됩니다.
    - `int` 값으로 해석할 때: b3가 LSB가 되고, b0가 MSB가 됩니다.
    - `int` 값은 `(b0 << 24) | (b1 << 16) | (b2 << 8) | b3`의 형태로 표현됩니다.

예를 들어, 각 바이트가 다음의 값이라고 가정해 보겠습니다:

- b0 = 0x01
- b1 = 0x02
- b2 = 0x03
- b3 = 0x04

그러면 다음과 같이 계산할 수 있습니다:

- **Little Endian 시스템에서** 읽었을 때의 `int` 값:
    
    - `0x04030201` (즉, `int` 값은 67,108,864)
- **Big Endian 시스템에서** 읽었을 때의 `int` 값:
    
    - `0x01020304` (즉, `int` 값은 16,909,060)

이러한 값의 차이는 시스템이 데이터의 바이트 순서를 어떻게 해석하는 지에 따라 달라집니다. 따라서, endian 방식이 다른 시스템 간에 데이터를 주고받을 때는, 데이터의 바이트 순서를 적절히 변환해주어야 호환성을 유지할 수 있습니다.
