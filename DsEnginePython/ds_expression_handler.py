import re
from enum import Enum
from ds_signal_handler import ds_status

class token_type(Enum):
    extract_name = 1,
    calculate_exp = 2

def tokenize(expStr, to_use):
    pat = re.compile(
        r'(?:(?<=[^\w\.\w])(?=\w)|(?=[^\w.\w]))'\
            if to_use == token_type.extract_name\
            else r'(?:(?<=[^\w\.\w%\w])(?=\w)|(?=[^\w.\w%\w]))', 
        re.MULTILINE
    )
    return [
        x 
        for x in re.sub(pat, ' ', expStr).split(' ') 
        if x
    ]

def parse_expr(expStr, to_use:token_type = token_type.extract_name):
    tokens = tokenize(expStr, to_use)
    op = dict(zip('!&|()', (2, 1, 1, 0, 0)))
    output = []
    stack = []

    for item in tokens:
        if item not in op:
            output.append(item)
        elif item == '(':
                stack.append(item)
        elif item == ')':
            while stack != [] and \
                stack[-1] != '(':
                output.append(stack.pop())
            stack.pop()
        else:
            while stack != [] and \
                    op[stack[-1]] >= op[item]:
                output.append(stack.pop())
            stack.append(item)

    while stack:
        output.append(stack.pop())
    return output

def token_status_spliter(item):
    split_res = item.split('%')
    return (split_res[0], ds_status.F) if len(split_res) == 1\
            else (split_res[0], ds_status(split_res[1]))
    
def calc_expr(tokens, in_signals, target_checker):
    operations = {
        '&': lambda x, y: y and x,
        '|': lambda x, y: y or x
    }
    conditions = {
        '!': lambda x: not x
    }

    stack = []
    for item in tokens:
        if item not in operations:
            if '!' in item:
                x = stack.pop()
                stack.append(conditions[item](x))
            else:
                name, target_status = token_status_spliter(item)
                name = name.replace(".Tag", "")
                stack.append(target_checker(in_signals[f"{name}.Tag"], target_status))
        else:
            x = stack.pop()
            y = stack.pop()
            stack.append(operations[item](x, y))

    return stack[-1]