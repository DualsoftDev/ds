```sql
CREATE TABLE [{Tn.TagKind}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [name]        NVARCHAR(64) UNIQUE NOT NULL CHECK(LENGTH(name) <= 64)
    , CONSTRAINT uniq_row UNIQUE (id, name)
);

```
- 프로그램 소스코드 컴파일타임에 결정되는 TagKind 값들을 갖는다.