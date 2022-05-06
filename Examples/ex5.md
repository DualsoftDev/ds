:smile: Welcome to the DS world  :smile:
# Example 5

## Automatic door system (Advance I/F) :door:


 ![AAA](./png/ex5.dio.png)
 
  - action list 
    1. Open
    2. Close

  ref to @sf (Start first)  [4.1 Priority operation](/Language/ds-language-table.md) 


```

  [sys]door  = {
          [task] t = { Detect;
              Open = { Out1 ~ In1, In2 }
              Close = { Out2 ~ _ }
          }
          [flow] f1 = {
                #(Detect) > @sf (Open);
                Open > @s(10)  > Close;
          }
          [flow] f2 = {  Open <||> Close }
  }
 
```
