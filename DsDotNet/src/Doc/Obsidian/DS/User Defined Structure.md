# Structure
- XGI 에서 `사용자 데이터 타입` 우클릭 후 새로 만들기
- Structure `MyData` 에 대해 다음 세개의 항목 선언 시 xml 구조
	- REAL height
	- REAL widht
	- int age 
- https://www.youtube.com/watch?v=StTWxmZ7xnA 참고
# Data type 선언부				
<UserDataTypes>

    <UserDataType Version="256" Attribute="0" Comment="" Find="1">Person<UserDataTypeVar Version="Ver 1.0" StructSize="320" StructSizeXGI="288" IncludeLibrary="0" MultiUserRunEdit="0" Count="3">

        <Symbols><Symbol Name="Age" Kind="0" Type="INT" State="0" Address="" Trigger="" InitValue="" Comment="" Device="T" DevicePos="272" TotalSize="16" OrderIndex="2" HMI="0" EIP="0" SturctureArrayOffset="0" ModuleInfo="" ArrayPointer="0" PtrType="" Motion="0"></Symbol>

                <Symbol Name="Gender" Kind="0" Type="BOOL" State="0" Address="" Trigger="" InitValue="" Comment="" Device="T" DevicePos="256" TotalSize="1" OrderIndex="1" HMI="0" EIP="0" SturctureArrayOffset="0" ModuleInfo="" ArrayPointer="0" PtrType="" Motion="0"></Symbol>

                <Symbol Name="Name" Kind="0" Type="STRING" State="0" Address="" Trigger="" InitValue="" Comment="" Device="T" DevicePos="0" TotalSize="256" OrderIndex="0" HMI="0" EIP="0" SturctureArrayOffset="0" ModuleInfo="" ArrayPointer="0" PtrType="" Motion="0"></Symbol>

            </Symbols>

        </UserDataTypeVar>

    </UserDataType>

</UserDataTypes>


# User Data 이용한 변수 선언
<GlobalVariables>

    <GlobalVariable Version="Ver 1.0" EIPGUID="779c7551-5051-4122-9361-7b4fa47337ba"

        EIPChangedID="0" HMIGUID="32483a8f-eb0d-4bcf-963b-3f2b1ecac46d" MultiUserRunEdit="0"

        MotionGUID="7533d6d7-aad0-4cfe-970f-f1a0b46ab17c" Count="2">

        <Symbols>

            <Symbol Name="Kim" Kind="6" Type="Person" State="0" Address="" Trigger="" InitValue="" Comment="" Device="A" DevicePos="0" TotalSize="320"

                OrderIndex="-1" HMI="0" EIP="0" SturctureArrayOffset="0" ModuleInfo="" ArrayPointer="0" PtrType="" Motion="0"></Symbol>

            <Symbol Name="Park" Kind="6" Type="Person" State="0" Address="" Trigger="" InitValue="" Comment="" Device="A" DevicePos="320" TotalSize="320"

                OrderIndex="-1" HMI="0" EIP="0" SturctureArrayOffset="0" ModuleInfo="" ArrayPointer="0" PtrType="" Motion="0"></Symbol>

        </Symbols>

        <TempVar Count="0"></TempVar>

        <HMIFlags></HMIFlags>

        <DirectVarComment Count="0"></DirectVarComment>

    </GlobalVariable>

</GlobalVariables>

- 
