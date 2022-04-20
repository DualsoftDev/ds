grammar dsFunctions;

import dsLexer;


funcSet: AT 'set' LPARENTHESIS segment RPARENTHESIS;
funcG: AT 'g' LPARENTHESIS segment RPARENTHESIS;
funcH: AT 'h' LPARENTHESIS segment RPARENTHESIS;

funcXOR: AT 'xor' LPARENTHESIS segment COMMA segment RPARENTHESIS;
funcNXOR: AT 'nxor' LPARENTHESIS segment COMMA segment RPARENTHESIS;
funcNAND: AT 'nand' LPARENTHESIS segment COMMA segment RPARENTHESIS;
funcNOR: AT 'nor' LPARENTHESIS segment COMMA segment RPARENTHESIS;


func
    : funcSet
    | funcG
    | funcH
    | funcXOR
    | funcNXOR
    | funcNAND
    | funcNOR
    ;
