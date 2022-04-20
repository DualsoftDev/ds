:smile: Welcome to the DS world  :smile:
# Example 3

## Automatic door system :door:


 ![AAA](./png/ex3.dio.png)
 
  - action list 
    1. Open
    2. Close

  ref to @sf (Start first)  [4.1 Priority operation](/Language/ds-language-table.md) 

```
 [sys]door  = { Open <||> Close;
               (Detect) > @sf (Open) > @s(10)  > Close;
  }
```
