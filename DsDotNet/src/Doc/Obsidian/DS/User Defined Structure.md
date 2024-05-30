# Structure
- XGI 에서 `사용자 데이터 타입` 우클릭 후 새로 만들기
- Structure `MyData` 에 대해 다음 세개의 항목 선언 시 xml 구조
	- REAL height
	- REAL widht
	- int age 
- https://www.youtube.com/watch?v=StTWxmZ7xnA 참고
				
                </Programs>

                <UserFunctions></UserFunctions>

                <UserDataTypes>

                    <UserDataType Version="256" Attribute="0" Comment="" Find="1">MyData

                        <UserDataTypeVar Version="Ver 1.0" StructSize="128" StructSizeXGI="80" IncludeLibrary="0" MultiUserRunEdit="0" Count="3">

                            <Symbols>

                                <Symbol Name="age" Kind="0" Type="INT" State="0" Address="" Trigger="" InitValue="" Comment="" Device="T" DevicePos="64" TotalSize="16" OrderIndex="2" HMI="0" EIP="0" SturctureArrayOffset="0" ModuleInfo="" ArrayPointer="0" PtrType="" Motion="0"></Symbol>

                                <Symbol Name="height" Kind="0" Type="REAL" State="0" Address="" Trigger="" InitValue="" Comment="" Device="T" DevicePos="0" TotalSize="32" OrderIndex="0" HMI="0" EIP="0" SturctureArrayOffset="0" ModuleInfo="" ArrayPointer="0" PtrType="" Motion="0"></Symbol>

                                <Symbol Name="width" Kind="0" Type="REAL" State="0" Address="" Trigger="" InitValue="" Comment="" Device="T" DevicePos="32" TotalSize="32" OrderIndex="1" HMI="0" EIP="0" SturctureArrayOffset="0" ModuleInfo="" ArrayPointer="0" PtrType="" Motion="0"></Symbol>

                            </Symbols>

                        </UserDataTypeVar>

                    </UserDataType>

                </UserDataTypes>

                <UserLibrary></UserLibrary>

            </POU>