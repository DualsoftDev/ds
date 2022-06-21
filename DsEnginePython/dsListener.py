# Generated from ds.g4 by ANTLR 4.10.1
from antlr4 import *
if __name__ is not None and "." in __name__:
    from .dsParser import dsParser
else:
    from dsParser import dsParser

# This class defines a complete listener for a parse tree produced by dsParser.
class dsListener(ParseTreeListener):

    # Enter a parse tree produced by dsParser#program.
    def enterProgram(self, ctx:dsParser.ProgramContext):
        pass

    # Exit a parse tree produced by dsParser#program.
    def exitProgram(self, ctx:dsParser.ProgramContext):
        pass


    # Enter a parse tree produced by dsParser#system.
    def enterSystem(self, ctx:dsParser.SystemContext):
        pass

    # Exit a parse tree produced by dsParser#system.
    def exitSystem(self, ctx:dsParser.SystemContext):
        pass


    # Enter a parse tree produced by dsParser#sysProp.
    def enterSysProp(self, ctx:dsParser.SysPropContext):
        pass

    # Exit a parse tree produced by dsParser#sysProp.
    def exitSysProp(self, ctx:dsParser.SysPropContext):
        pass


    # Enter a parse tree produced by dsParser#sysBlock.
    def enterSysBlock(self, ctx:dsParser.SysBlockContext):
        pass

    # Exit a parse tree produced by dsParser#sysBlock.
    def exitSysBlock(self, ctx:dsParser.SysBlockContext):
        pass


    # Enter a parse tree produced by dsParser#task.
    def enterTask(self, ctx:dsParser.TaskContext):
        pass

    # Exit a parse tree produced by dsParser#task.
    def exitTask(self, ctx:dsParser.TaskContext):
        pass


    # Enter a parse tree produced by dsParser#taskProp.
    def enterTaskProp(self, ctx:dsParser.TaskPropContext):
        pass

    # Exit a parse tree produced by dsParser#taskProp.
    def exitTaskProp(self, ctx:dsParser.TaskPropContext):
        pass


    # Enter a parse tree produced by dsParser#flow.
    def enterFlow(self, ctx:dsParser.FlowContext):
        pass

    # Exit a parse tree produced by dsParser#flow.
    def exitFlow(self, ctx:dsParser.FlowContext):
        pass


    # Enter a parse tree produced by dsParser#flowProp.
    def enterFlowProp(self, ctx:dsParser.FlowPropContext):
        pass

    # Exit a parse tree produced by dsParser#flowProp.
    def exitFlowProp(self, ctx:dsParser.FlowPropContext):
        pass


    # Enter a parse tree produced by dsParser#id.
    def enterId(self, ctx:dsParser.IdContext):
        pass

    # Exit a parse tree produced by dsParser#id.
    def exitId(self, ctx:dsParser.IdContext):
        pass


    # Enter a parse tree produced by dsParser#acc.
    def enterAcc(self, ctx:dsParser.AccContext):
        pass

    # Exit a parse tree produced by dsParser#acc.
    def exitAcc(self, ctx:dsParser.AccContext):
        pass


    # Enter a parse tree produced by dsParser#listing.
    def enterListing(self, ctx:dsParser.ListingContext):
        pass

    # Exit a parse tree produced by dsParser#listing.
    def exitListing(self, ctx:dsParser.ListingContext):
        pass


    # Enter a parse tree produced by dsParser#call_listing.
    def enterCall_listing(self, ctx:dsParser.Call_listingContext):
        pass

    # Exit a parse tree produced by dsParser#call_listing.
    def exitCall_listing(self, ctx:dsParser.Call_listingContext):
        pass


    # Enter a parse tree produced by dsParser#parenting.
    def enterParenting(self, ctx:dsParser.ParentingContext):
        pass

    # Exit a parse tree produced by dsParser#parenting.
    def exitParenting(self, ctx:dsParser.ParentingContext):
        pass


    # Enter a parse tree produced by dsParser#macro.
    def enterMacro(self, ctx:dsParser.MacroContext):
        pass

    # Exit a parse tree produced by dsParser#macro.
    def exitMacro(self, ctx:dsParser.MacroContext):
        pass


    # Enter a parse tree produced by dsParser#macroHeader.
    def enterMacroHeader(self, ctx:dsParser.MacroHeaderContext):
        pass

    # Exit a parse tree produced by dsParser#macroHeader.
    def exitMacroHeader(self, ctx:dsParser.MacroHeaderContext):
        pass


    # Enter a parse tree produced by dsParser#simpleMacroHeader.
    def enterSimpleMacroHeader(self, ctx:dsParser.SimpleMacroHeaderContext):
        pass

    # Exit a parse tree produced by dsParser#simpleMacroHeader.
    def exitSimpleMacroHeader(self, ctx:dsParser.SimpleMacroHeaderContext):
        pass


    # Enter a parse tree produced by dsParser#namedMacroHeader.
    def enterNamedMacroHeader(self, ctx:dsParser.NamedMacroHeaderContext):
        pass

    # Exit a parse tree produced by dsParser#namedMacroHeader.
    def exitNamedMacroHeader(self, ctx:dsParser.NamedMacroHeaderContext):
        pass


    # Enter a parse tree produced by dsParser#call.
    def enterCall(self, ctx:dsParser.CallContext):
        pass

    # Exit a parse tree produced by dsParser#call.
    def exitCall(self, ctx:dsParser.CallContext):
        pass


    # Enter a parse tree produced by dsParser#callPhrase.
    def enterCallPhrase(self, ctx:dsParser.CallPhraseContext):
        pass

    # Exit a parse tree produced by dsParser#callPhrase.
    def exitCallPhrase(self, ctx:dsParser.CallPhraseContext):
        pass


    # Enter a parse tree produced by dsParser#causal.
    def enterCausal(self, ctx:dsParser.CausalContext):
        pass

    # Exit a parse tree produced by dsParser#causal.
    def exitCausal(self, ctx:dsParser.CausalContext):
        pass


    # Enter a parse tree produced by dsParser#causals.
    def enterCausals(self, ctx:dsParser.CausalsContext):
        pass

    # Exit a parse tree produced by dsParser#causals.
    def exitCausals(self, ctx:dsParser.CausalsContext):
        pass


    # Enter a parse tree produced by dsParser#importStatements.
    def enterImportStatements(self, ctx:dsParser.ImportStatementsContext):
        pass

    # Exit a parse tree produced by dsParser#importStatements.
    def exitImportStatements(self, ctx:dsParser.ImportStatementsContext):
        pass


    # Enter a parse tree produced by dsParser#expressions.
    def enterExpressions(self, ctx:dsParser.ExpressionsContext):
        pass

    # Exit a parse tree produced by dsParser#expressions.
    def exitExpressions(self, ctx:dsParser.ExpressionsContext):
        pass


    # Enter a parse tree produced by dsParser#calls.
    def enterCalls(self, ctx:dsParser.CallsContext):
        pass

    # Exit a parse tree produced by dsParser#calls.
    def exitCalls(self, ctx:dsParser.CallsContext):
        pass


    # Enter a parse tree produced by dsParser#causalPhrase.
    def enterCausalPhrase(self, ctx:dsParser.CausalPhraseContext):
        pass

    # Exit a parse tree produced by dsParser#causalPhrase.
    def exitCausalPhrase(self, ctx:dsParser.CausalPhraseContext):
        pass


    # Enter a parse tree produced by dsParser#causalToken.
    def enterCausalToken(self, ctx:dsParser.CausalTokenContext):
        pass

    # Exit a parse tree produced by dsParser#causalToken.
    def exitCausalToken(self, ctx:dsParser.CausalTokenContext):
        pass


    # Enter a parse tree produced by dsParser#segmentValue.
    def enterSegmentValue(self, ctx:dsParser.SegmentValueContext):
        pass

    # Exit a parse tree produced by dsParser#segmentValue.
    def exitSegmentValue(self, ctx:dsParser.SegmentValueContext):
        pass


    # Enter a parse tree produced by dsParser#causalTokensDNF.
    def enterCausalTokensDNF(self, ctx:dsParser.CausalTokensDNFContext):
        pass

    # Exit a parse tree produced by dsParser#causalTokensDNF.
    def exitCausalTokensDNF(self, ctx:dsParser.CausalTokensDNFContext):
        pass


    # Enter a parse tree produced by dsParser#causalTokensCNF.
    def enterCausalTokensCNF(self, ctx:dsParser.CausalTokensCNFContext):
        pass

    # Exit a parse tree produced by dsParser#causalTokensCNF.
    def exitCausalTokensCNF(self, ctx:dsParser.CausalTokensCNFContext):
        pass


    # Enter a parse tree produced by dsParser#importFinal.
    def enterImportFinal(self, ctx:dsParser.ImportFinalContext):
        pass

    # Exit a parse tree produced by dsParser#importFinal.
    def exitImportFinal(self, ctx:dsParser.ImportFinalContext):
        pass


    # Enter a parse tree produced by dsParser#importStatement.
    def enterImportStatement(self, ctx:dsParser.ImportStatementContext):
        pass

    # Exit a parse tree produced by dsParser#importStatement.
    def exitImportStatement(self, ctx:dsParser.ImportStatementContext):
        pass


    # Enter a parse tree produced by dsParser#importAs.
    def enterImportAs(self, ctx:dsParser.ImportAsContext):
        pass

    # Exit a parse tree produced by dsParser#importAs.
    def exitImportAs(self, ctx:dsParser.ImportAsContext):
        pass


    # Enter a parse tree produced by dsParser#importPhrase.
    def enterImportPhrase(self, ctx:dsParser.ImportPhraseContext):
        pass

    # Exit a parse tree produced by dsParser#importPhrase.
    def exitImportPhrase(self, ctx:dsParser.ImportPhraseContext):
        pass


    # Enter a parse tree produced by dsParser#importSystemName.
    def enterImportSystemName(self, ctx:dsParser.ImportSystemNameContext):
        pass

    # Exit a parse tree produced by dsParser#importSystemName.
    def exitImportSystemName(self, ctx:dsParser.ImportSystemNameContext):
        pass


    # Enter a parse tree produced by dsParser#importAlias.
    def enterImportAlias(self, ctx:dsParser.ImportAliasContext):
        pass

    # Exit a parse tree produced by dsParser#importAlias.
    def exitImportAlias(self, ctx:dsParser.ImportAliasContext):
        pass


    # Enter a parse tree produced by dsParser#quotedFilePath.
    def enterQuotedFilePath(self, ctx:dsParser.QuotedFilePathContext):
        pass

    # Exit a parse tree produced by dsParser#quotedFilePath.
    def exitQuotedFilePath(self, ctx:dsParser.QuotedFilePathContext):
        pass


    # Enter a parse tree produced by dsParser#causalOperator.
    def enterCausalOperator(self, ctx:dsParser.CausalOperatorContext):
        pass

    # Exit a parse tree produced by dsParser#causalOperator.
    def exitCausalOperator(self, ctx:dsParser.CausalOperatorContext):
        pass


    # Enter a parse tree produced by dsParser#sys_.
    def enterSys_(self, ctx:dsParser.Sys_Context):
        pass

    # Exit a parse tree produced by dsParser#sys_.
    def exitSys_(self, ctx:dsParser.Sys_Context):
        pass


    # Enter a parse tree produced by dsParser#logicalBinaryOperator.
    def enterLogicalBinaryOperator(self, ctx:dsParser.LogicalBinaryOperatorContext):
        pass

    # Exit a parse tree produced by dsParser#logicalBinaryOperator.
    def exitLogicalBinaryOperator(self, ctx:dsParser.LogicalBinaryOperatorContext):
        pass


    # Enter a parse tree produced by dsParser#expression.
    def enterExpression(self, ctx:dsParser.ExpressionContext):
        pass

    # Exit a parse tree produced by dsParser#expression.
    def exitExpression(self, ctx:dsParser.ExpressionContext):
        pass


    # Enter a parse tree produced by dsParser#number.
    def enterNumber(self, ctx:dsParser.NumberContext):
        pass

    # Exit a parse tree produced by dsParser#number.
    def exitNumber(self, ctx:dsParser.NumberContext):
        pass


    # Enter a parse tree produced by dsParser#string.
    def enterString(self, ctx:dsParser.StringContext):
        pass

    # Exit a parse tree produced by dsParser#string.
    def exitString(self, ctx:dsParser.StringContext):
        pass


    # Enter a parse tree produced by dsParser#segValue.
    def enterSegValue(self, ctx:dsParser.SegValueContext):
        pass

    # Exit a parse tree produced by dsParser#segValue.
    def exitSegValue(self, ctx:dsParser.SegValueContext):
        pass


    # Enter a parse tree produced by dsParser#value.
    def enterValue(self, ctx:dsParser.ValueContext):
        pass

    # Exit a parse tree produced by dsParser#value.
    def exitValue(self, ctx:dsParser.ValueContext):
        pass


    # Enter a parse tree produced by dsParser#funcSet.
    def enterFuncSet(self, ctx:dsParser.FuncSetContext):
        pass

    # Exit a parse tree produced by dsParser#funcSet.
    def exitFuncSet(self, ctx:dsParser.FuncSetContext):
        pass


    # Enter a parse tree produced by dsParser#funcG.
    def enterFuncG(self, ctx:dsParser.FuncGContext):
        pass

    # Exit a parse tree produced by dsParser#funcG.
    def exitFuncG(self, ctx:dsParser.FuncGContext):
        pass


    # Enter a parse tree produced by dsParser#funcH.
    def enterFuncH(self, ctx:dsParser.FuncHContext):
        pass

    # Exit a parse tree produced by dsParser#funcH.
    def exitFuncH(self, ctx:dsParser.FuncHContext):
        pass


    # Enter a parse tree produced by dsParser#funcLatch.
    def enterFuncLatch(self, ctx:dsParser.FuncLatchContext):
        pass

    # Exit a parse tree produced by dsParser#funcLatch.
    def exitFuncLatch(self, ctx:dsParser.FuncLatchContext):
        pass


    # Enter a parse tree produced by dsParser#funcXOR.
    def enterFuncXOR(self, ctx:dsParser.FuncXORContext):
        pass

    # Exit a parse tree produced by dsParser#funcXOR.
    def exitFuncXOR(self, ctx:dsParser.FuncXORContext):
        pass


    # Enter a parse tree produced by dsParser#funcNXOR.
    def enterFuncNXOR(self, ctx:dsParser.FuncNXORContext):
        pass

    # Exit a parse tree produced by dsParser#funcNXOR.
    def exitFuncNXOR(self, ctx:dsParser.FuncNXORContext):
        pass


    # Enter a parse tree produced by dsParser#funcNAND.
    def enterFuncNAND(self, ctx:dsParser.FuncNANDContext):
        pass

    # Exit a parse tree produced by dsParser#funcNAND.
    def exitFuncNAND(self, ctx:dsParser.FuncNANDContext):
        pass


    # Enter a parse tree produced by dsParser#funcNOR.
    def enterFuncNOR(self, ctx:dsParser.FuncNORContext):
        pass

    # Exit a parse tree produced by dsParser#funcNOR.
    def exitFuncNOR(self, ctx:dsParser.FuncNORContext):
        pass


    # Enter a parse tree produced by dsParser#funcExpression.
    def enterFuncExpression(self, ctx:dsParser.FuncExpressionContext):
        pass

    # Exit a parse tree produced by dsParser#funcExpression.
    def exitFuncExpression(self, ctx:dsParser.FuncExpressionContext):
        pass


    # Enter a parse tree produced by dsParser#funcConvNum.
    def enterFuncConvNum(self, ctx:dsParser.FuncConvNumContext):
        pass

    # Exit a parse tree produced by dsParser#funcConvNum.
    def exitFuncConvNum(self, ctx:dsParser.FuncConvNumContext):
        pass


    # Enter a parse tree produced by dsParser#funcConvStr.
    def enterFuncConvStr(self, ctx:dsParser.FuncConvStrContext):
        pass

    # Exit a parse tree produced by dsParser#funcConvStr.
    def exitFuncConvStr(self, ctx:dsParser.FuncConvStrContext):
        pass


    # Enter a parse tree produced by dsParser#funcConvBCD.
    def enterFuncConvBCD(self, ctx:dsParser.FuncConvBCDContext):
        pass

    # Exit a parse tree produced by dsParser#funcConvBCD.
    def exitFuncConvBCD(self, ctx:dsParser.FuncConvBCDContext):
        pass


    # Enter a parse tree produced by dsParser#funcConvBin.
    def enterFuncConvBin(self, ctx:dsParser.FuncConvBinContext):
        pass

    # Exit a parse tree produced by dsParser#funcConvBin.
    def exitFuncConvBin(self, ctx:dsParser.FuncConvBinContext):
        pass


    # Enter a parse tree produced by dsParser#funcConvAbs.
    def enterFuncConvAbs(self, ctx:dsParser.FuncConvAbsContext):
        pass

    # Exit a parse tree produced by dsParser#funcConvAbs.
    def exitFuncConvAbs(self, ctx:dsParser.FuncConvAbsContext):
        pass


    # Enter a parse tree produced by dsParser#funcConvSin.
    def enterFuncConvSin(self, ctx:dsParser.FuncConvSinContext):
        pass

    # Exit a parse tree produced by dsParser#funcConvSin.
    def exitFuncConvSin(self, ctx:dsParser.FuncConvSinContext):
        pass


    # Enter a parse tree produced by dsParser#funcConvCos.
    def enterFuncConvCos(self, ctx:dsParser.FuncConvCosContext):
        pass

    # Exit a parse tree produced by dsParser#funcConvCos.
    def exitFuncConvCos(self, ctx:dsParser.FuncConvCosContext):
        pass


    # Enter a parse tree produced by dsParser#funcConvRound.
    def enterFuncConvRound(self, ctx:dsParser.FuncConvRoundContext):
        pass

    # Exit a parse tree produced by dsParser#funcConvRound.
    def exitFuncConvRound(self, ctx:dsParser.FuncConvRoundContext):
        pass


    # Enter a parse tree produced by dsParser#funcSysToggleMs.
    def enterFuncSysToggleMs(self, ctx:dsParser.FuncSysToggleMsContext):
        pass

    # Exit a parse tree produced by dsParser#funcSysToggleMs.
    def exitFuncSysToggleMs(self, ctx:dsParser.FuncSysToggleMsContext):
        pass


    # Enter a parse tree produced by dsParser#funcSysToggleS.
    def enterFuncSysToggleS(self, ctx:dsParser.FuncSysToggleSContext):
        pass

    # Exit a parse tree produced by dsParser#funcSysToggleS.
    def exitFuncSysToggleS(self, ctx:dsParser.FuncSysToggleSContext):
        pass


    # Enter a parse tree produced by dsParser#funcSysCurrentTime.
    def enterFuncSysCurrentTime(self, ctx:dsParser.FuncSysCurrentTimeContext):
        pass

    # Exit a parse tree produced by dsParser#funcSysCurrentTime.
    def exitFuncSysCurrentTime(self, ctx:dsParser.FuncSysCurrentTimeContext):
        pass


    # Enter a parse tree produced by dsParser#func.
    def enterFunc(self, ctx:dsParser.FuncContext):
        pass

    # Exit a parse tree produced by dsParser#func.
    def exitFunc(self, ctx:dsParser.FuncContext):
        pass


    # Enter a parse tree produced by dsParser#proc.
    def enterProc(self, ctx:dsParser.ProcContext):
        pass

    # Exit a parse tree produced by dsParser#proc.
    def exitProc(self, ctx:dsParser.ProcContext):
        pass


    # Enter a parse tree produced by dsParser#procAssign.
    def enterProcAssign(self, ctx:dsParser.ProcAssignContext):
        pass

    # Exit a parse tree produced by dsParser#procAssign.
    def exitProcAssign(self, ctx:dsParser.ProcAssignContext):
        pass


    # Enter a parse tree produced by dsParser#procSleepMs.
    def enterProcSleepMs(self, ctx:dsParser.ProcSleepMsContext):
        pass

    # Exit a parse tree produced by dsParser#procSleepMs.
    def exitProcSleepMs(self, ctx:dsParser.ProcSleepMsContext):
        pass


    # Enter a parse tree produced by dsParser#procSleepS.
    def enterProcSleepS(self, ctx:dsParser.ProcSleepSContext):
        pass

    # Exit a parse tree produced by dsParser#procSleepS.
    def exitProcSleepS(self, ctx:dsParser.ProcSleepSContext):
        pass


    # Enter a parse tree produced by dsParser#procStartFirst.
    def enterProcStartFirst(self, ctx:dsParser.ProcStartFirstContext):
        pass

    # Exit a parse tree produced by dsParser#procStartFirst.
    def exitProcStartFirst(self, ctx:dsParser.ProcStartFirstContext):
        pass


    # Enter a parse tree produced by dsParser#procLastFirst.
    def enterProcLastFirst(self, ctx:dsParser.ProcLastFirstContext):
        pass

    # Exit a parse tree produced by dsParser#procLastFirst.
    def exitProcLastFirst(self, ctx:dsParser.ProcLastFirstContext):
        pass


    # Enter a parse tree produced by dsParser#procPushStart.
    def enterProcPushStart(self, ctx:dsParser.ProcPushStartContext):
        pass

    # Exit a parse tree produced by dsParser#procPushStart.
    def exitProcPushStart(self, ctx:dsParser.ProcPushStartContext):
        pass


    # Enter a parse tree produced by dsParser#procPushReset.
    def enterProcPushReset(self, ctx:dsParser.ProcPushResetContext):
        pass

    # Exit a parse tree produced by dsParser#procPushReset.
    def exitProcPushReset(self, ctx:dsParser.ProcPushResetContext):
        pass


    # Enter a parse tree produced by dsParser#procPushStartReset.
    def enterProcPushStartReset(self, ctx:dsParser.ProcPushStartResetContext):
        pass

    # Exit a parse tree produced by dsParser#procPushStartReset.
    def exitProcPushStartReset(self, ctx:dsParser.ProcPushStartResetContext):
        pass


    # Enter a parse tree produced by dsParser#procOnlyStart.
    def enterProcOnlyStart(self, ctx:dsParser.ProcOnlyStartContext):
        pass

    # Exit a parse tree produced by dsParser#procOnlyStart.
    def exitProcOnlyStart(self, ctx:dsParser.ProcOnlyStartContext):
        pass


    # Enter a parse tree produced by dsParser#procOnlyReset.
    def enterProcOnlyReset(self, ctx:dsParser.ProcOnlyResetContext):
        pass

    # Exit a parse tree produced by dsParser#procOnlyReset.
    def exitProcOnlyReset(self, ctx:dsParser.ProcOnlyResetContext):
        pass


    # Enter a parse tree produced by dsParser#procSelfStart.
    def enterProcSelfStart(self, ctx:dsParser.ProcSelfStartContext):
        pass

    # Exit a parse tree produced by dsParser#procSelfStart.
    def exitProcSelfStart(self, ctx:dsParser.ProcSelfStartContext):
        pass


    # Enter a parse tree produced by dsParser#procSelfReset.
    def enterProcSelfReset(self, ctx:dsParser.ProcSelfResetContext):
        pass

    # Exit a parse tree produced by dsParser#procSelfReset.
    def exitProcSelfReset(self, ctx:dsParser.ProcSelfResetContext):
        pass


    # Enter a parse tree produced by dsParser#segments.
    def enterSegments(self, ctx:dsParser.SegmentsContext):
        pass

    # Exit a parse tree produced by dsParser#segments.
    def exitSegments(self, ctx:dsParser.SegmentsContext):
        pass


    # Enter a parse tree produced by dsParser#segment.
    def enterSegment(self, ctx:dsParser.SegmentContext):
        pass

    # Exit a parse tree produced by dsParser#segment.
    def exitSegment(self, ctx:dsParser.SegmentContext):
        pass


    # Enter a parse tree produced by dsParser#segmentsCNF.
    def enterSegmentsCNF(self, ctx:dsParser.SegmentsCNFContext):
        pass

    # Exit a parse tree produced by dsParser#segmentsCNF.
    def exitSegmentsCNF(self, ctx:dsParser.SegmentsCNFContext):
        pass


    # Enter a parse tree produced by dsParser#segmentsDNF.
    def enterSegmentsDNF(self, ctx:dsParser.SegmentsDNFContext):
        pass

    # Exit a parse tree produced by dsParser#segmentsDNF.
    def exitSegmentsDNF(self, ctx:dsParser.SegmentsDNFContext):
        pass


    # Enter a parse tree produced by dsParser#comment.
    def enterComment(self, ctx:dsParser.CommentContext):
        pass

    # Exit a parse tree produced by dsParser#comment.
    def exitComment(self, ctx:dsParser.CommentContext):
        pass



del dsParser