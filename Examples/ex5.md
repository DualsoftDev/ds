:smile: Welcome to the DS world  :smile:
# Example 5

## Automatic door system (Advance I/F) :door:


 ![AAA](./png/ex5.dio.png)
 
  - action list 
    1. Open
    2. Close

  ref to @sf (Start first)  [4.1 Priority operation](/Language/ds-language-table.md) 


```
 [sys]door  = {  Open <||> Close;
               (Detect) > @sf (Open) > @s(10)  > Close; 
               //@sf is Start Priority 

      Open = { Out1 ~ In1, In2 }
      Close = { Out2 ~ _ }
  }
```
