:smile: Welcome to the DS world  :smile:
# Example 3

## Automatic door system :door:


 ![AAA](./png/ex3.dio.png)
 
  - action list 
    1. Open
    2. Close

```

  [sys]door  = {
          [task] t = { Detect;  }
          [flow] f1 = {
                #(Detect) > @sf (Open);
                Open > @s(10)  > Close;
          }
          [flow] f2 = {  Open <||> Close }
  }
```
