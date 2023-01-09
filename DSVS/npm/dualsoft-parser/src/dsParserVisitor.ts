// Generated from ../../../Grammar/g4s/dsParser.g4 by ANTLR 4.9.0-SNAPSHOT


import { ParseTreeVisitor } from "antlr4ts/tree/ParseTreeVisitor";

import { CommentContext } from "./dsParser";
import { SystemContext } from "./dsParser";
import { SysHeaderContext } from "./dsParser";
import { SysBlockContext } from "./dsParser";
import { SystemNameContext } from "./dsParser";
import { IpSpecContext } from "./dsParser";
import { HostContext } from "./dsParser";
import { EtcNameContext } from "./dsParser";
import { Ipv4Context } from "./dsParser";
import { LoadDeviceBlockContext } from "./dsParser";
import { DeviceNameContext } from "./dsParser";
import { FileSpecContext } from "./dsParser";
import { EtcName1Context } from "./dsParser";
import { FilePathContext } from "./dsParser";
import { LoadExternalSystemBlockContext } from "./dsParser";
import { ExternalSystemNameContext } from "./dsParser";
import { AddressInOutContext } from "./dsParser";
import { InAddrContext } from "./dsParser";
import { OutAddrContext } from "./dsParser";
import { AddressItemContext } from "./dsParser";
import { TagAddressContext } from "./dsParser";
import { FunAddressContext } from "./dsParser";
import { PropsBlockContext } from "./dsParser";
import { SafetyBlockContext } from "./dsParser";
import { SafetyDefContext } from "./dsParser";
import { SafetyKeyContext } from "./dsParser";
import { SafetyValuesContext } from "./dsParser";
import { LayoutBlockContext } from "./dsParser";
import { PositionDefContext } from "./dsParser";
import { CallNameContext } from "./dsParser";
import { XywhContext } from "./dsParser";
import { XContext } from "./dsParser";
import { YContext } from "./dsParser";
import { WContext } from "./dsParser";
import { HContext } from "./dsParser";
import { FlowBlockContext } from "./dsParser";
import { ParentingBlockContext } from "./dsParser";
import { Identifier12ListingContext } from "./dsParser";
import { Identifier1ListingContext } from "./dsParser";
import { Identifier2ListingContext } from "./dsParser";
import { AliasBlockContext } from "./dsParser";
import { AliasListingContext } from "./dsParser";
import { AliasDefContext } from "./dsParser";
import { AliasMnemonicContext } from "./dsParser";
import { JobBlockContext } from "./dsParser";
import { CallListingContext } from "./dsParser";
import { JobNameContext } from "./dsParser";
import { CallApiDefContext } from "./dsParser";
import { CallKeyContext } from "./dsParser";
import { InterfaceBlockContext } from "./dsParser";
import { InterfaceListingContext } from "./dsParser";
import { InterfaceDefContext } from "./dsParser";
import { InterfaceNameContext } from "./dsParser";
import { SerPhraseContext } from "./dsParser";
import { CallComponentsContext } from "./dsParser";
import { InterfaceResetDefContext } from "./dsParser";
import { ButtonsBlocksContext } from "./dsParser";
import { EmergencyButtonBlockContext } from "./dsParser";
import { AutoButtonBlockContext } from "./dsParser";
import { ClearButtonBlockContext } from "./dsParser";
import { ManualButtonBlockContext } from "./dsParser";
import { StopButtonBlockContext } from "./dsParser";
import { RunButtonBlockContext } from "./dsParser";
import { DryrunButtonBlockContext } from "./dsParser";
import { HomeButtonBlockContext } from "./dsParser";
import { ButtonBlockContext } from "./dsParser";
import { ButtonDefContext } from "./dsParser";
import { BtnNameAddrContext } from "./dsParser";
import { ButtonNameContext } from "./dsParser";
import { FlowNameContext } from "./dsParser";
import { LampBlocksContext } from "./dsParser";
import { RunLampBlockContext } from "./dsParser";
import { DryrunLampBlockContext } from "./dsParser";
import { ManualLampBlockContext } from "./dsParser";
import { StopLampBlockContext } from "./dsParser";
import { EmgLampBlockContext } from "./dsParser";
import { LampBlockContext } from "./dsParser";
import { LampDefContext } from "./dsParser";
import { AddrDefContext } from "./dsParser";
import { LampNameContext } from "./dsParser";
import { CausalContext } from "./dsParser";
import { CausalPhraseContext } from "./dsParser";
import { CausalTokensCNFContext } from "./dsParser";
import { CausalTokenContext } from "./dsParser";
import { CausalOperatorContext } from "./dsParser";
import { CausalOperatorResetContext } from "./dsParser";
import { Identifier1234Context } from "./dsParser";
import { Identifier1Context } from "./dsParser";
import { Identifier2Context } from "./dsParser";
import { Identifier3Context } from "./dsParser";
import { Identifier4Context } from "./dsParser";
import { Identifier12Context } from "./dsParser";
import { Identifier23Context } from "./dsParser";
import { Identifier123Context } from "./dsParser";
import { Identifier123CNFContext } from "./dsParser";
import { FlowPathContext } from "./dsParser";
import { CodeBlockContext } from "./dsParser";
import { VariableBlockContext } from "./dsParser";
import { VariableDefContext } from "./dsParser";
import { VarNameContext } from "./dsParser";
import { ArgumentGroupsContext } from "./dsParser";
import { ArgumentGroupContext } from "./dsParser";
import { ArgumentContext } from "./dsParser";
import { VarIdentifierContext } from "./dsParser";
import { IntValueContext } from "./dsParser";
import { FloatValueContext } from "./dsParser";
import { VarTypeContext } from "./dsParser";
import { FunApplicationContext } from "./dsParser";
import { CommandBlockContext } from "./dsParser";
import { CommandDefContext } from "./dsParser";
import { CmdNameContext } from "./dsParser";
import { FunNameContext } from "./dsParser";
import { ObserveBlockContext } from "./dsParser";
import { ObserveDefContext } from "./dsParser";
import { ObserveNameContext } from "./dsParser";


/**
 * This interface defines a complete generic visitor for a parse tree produced
 * by `dsParser`.
 *
 * @param <Result> The return type of the visit operation. Use `void` for
 * operations with no return type.
 */
export interface dsParserVisitor<Result> extends ParseTreeVisitor<Result> {
	/**
	 * Visit a parse tree produced by `dsParser.comment`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitComment?: (ctx: CommentContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.system`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitSystem?: (ctx: SystemContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.sysHeader`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitSysHeader?: (ctx: SysHeaderContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.sysBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitSysBlock?: (ctx: SysBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.systemName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitSystemName?: (ctx: SystemNameContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.ipSpec`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitIpSpec?: (ctx: IpSpecContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.host`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitHost?: (ctx: HostContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.etcName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitEtcName?: (ctx: EtcNameContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.ipv4`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitIpv4?: (ctx: Ipv4Context) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.loadDeviceBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLoadDeviceBlock?: (ctx: LoadDeviceBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.deviceName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitDeviceName?: (ctx: DeviceNameContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.fileSpec`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitFileSpec?: (ctx: FileSpecContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.etcName1`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitEtcName1?: (ctx: EtcName1Context) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.filePath`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitFilePath?: (ctx: FilePathContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.loadExternalSystemBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLoadExternalSystemBlock?: (ctx: LoadExternalSystemBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.externalSystemName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitExternalSystemName?: (ctx: ExternalSystemNameContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.addressInOut`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitAddressInOut?: (ctx: AddressInOutContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.inAddr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitInAddr?: (ctx: InAddrContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.outAddr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitOutAddr?: (ctx: OutAddrContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.addressItem`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitAddressItem?: (ctx: AddressItemContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.tagAddress`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitTagAddress?: (ctx: TagAddressContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.funAddress`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitFunAddress?: (ctx: FunAddressContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.propsBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitPropsBlock?: (ctx: PropsBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.safetyBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitSafetyBlock?: (ctx: SafetyBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.safetyDef`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitSafetyDef?: (ctx: SafetyDefContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.safetyKey`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitSafetyKey?: (ctx: SafetyKeyContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.safetyValues`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitSafetyValues?: (ctx: SafetyValuesContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.layoutBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLayoutBlock?: (ctx: LayoutBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.positionDef`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitPositionDef?: (ctx: PositionDefContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.callName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCallName?: (ctx: CallNameContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.xywh`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitXywh?: (ctx: XywhContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.x`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitX?: (ctx: XContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.y`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitY?: (ctx: YContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.w`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitW?: (ctx: WContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.h`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitH?: (ctx: HContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.flowBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitFlowBlock?: (ctx: FlowBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.parentingBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitParentingBlock?: (ctx: ParentingBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.identifier12Listing`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitIdentifier12Listing?: (ctx: Identifier12ListingContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.identifier1Listing`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitIdentifier1Listing?: (ctx: Identifier1ListingContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.identifier2Listing`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitIdentifier2Listing?: (ctx: Identifier2ListingContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.aliasBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitAliasBlock?: (ctx: AliasBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.aliasListing`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitAliasListing?: (ctx: AliasListingContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.aliasDef`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitAliasDef?: (ctx: AliasDefContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.aliasMnemonic`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitAliasMnemonic?: (ctx: AliasMnemonicContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.jobBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitJobBlock?: (ctx: JobBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.callListing`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCallListing?: (ctx: CallListingContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.jobName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitJobName?: (ctx: JobNameContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.callApiDef`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCallApiDef?: (ctx: CallApiDefContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.callKey`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCallKey?: (ctx: CallKeyContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.interfaceBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitInterfaceBlock?: (ctx: InterfaceBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.interfaceListing`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitInterfaceListing?: (ctx: InterfaceListingContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.interfaceDef`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitInterfaceDef?: (ctx: InterfaceDefContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.interfaceName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitInterfaceName?: (ctx: InterfaceNameContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.serPhrase`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitSerPhrase?: (ctx: SerPhraseContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.callComponents`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCallComponents?: (ctx: CallComponentsContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.interfaceResetDef`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitInterfaceResetDef?: (ctx: InterfaceResetDefContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.buttonsBlocks`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitButtonsBlocks?: (ctx: ButtonsBlocksContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.emergencyButtonBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitEmergencyButtonBlock?: (ctx: EmergencyButtonBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.autoButtonBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitAutoButtonBlock?: (ctx: AutoButtonBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.clearButtonBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitClearButtonBlock?: (ctx: ClearButtonBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.manualButtonBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitManualButtonBlock?: (ctx: ManualButtonBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.stopButtonBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitStopButtonBlock?: (ctx: StopButtonBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.runButtonBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitRunButtonBlock?: (ctx: RunButtonBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.dryrunButtonBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitDryrunButtonBlock?: (ctx: DryrunButtonBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.homeButtonBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitHomeButtonBlock?: (ctx: HomeButtonBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.buttonBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitButtonBlock?: (ctx: ButtonBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.buttonDef`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitButtonDef?: (ctx: ButtonDefContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.btnNameAddr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBtnNameAddr?: (ctx: BtnNameAddrContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.buttonName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitButtonName?: (ctx: ButtonNameContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.flowName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitFlowName?: (ctx: FlowNameContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.lampBlocks`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLampBlocks?: (ctx: LampBlocksContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.runLampBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitRunLampBlock?: (ctx: RunLampBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.dryrunLampBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitDryrunLampBlock?: (ctx: DryrunLampBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.manualLampBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitManualLampBlock?: (ctx: ManualLampBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.stopLampBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitStopLampBlock?: (ctx: StopLampBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.emgLampBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitEmgLampBlock?: (ctx: EmgLampBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.lampBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLampBlock?: (ctx: LampBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.lampDef`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLampDef?: (ctx: LampDefContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.addrDef`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitAddrDef?: (ctx: AddrDefContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.lampName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLampName?: (ctx: LampNameContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.causal`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCausal?: (ctx: CausalContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.causalPhrase`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCausalPhrase?: (ctx: CausalPhraseContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.causalTokensCNF`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCausalTokensCNF?: (ctx: CausalTokensCNFContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.causalToken`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCausalToken?: (ctx: CausalTokenContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.causalOperator`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCausalOperator?: (ctx: CausalOperatorContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.causalOperatorReset`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCausalOperatorReset?: (ctx: CausalOperatorResetContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.identifier1234`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitIdentifier1234?: (ctx: Identifier1234Context) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.identifier1`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitIdentifier1?: (ctx: Identifier1Context) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.identifier2`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitIdentifier2?: (ctx: Identifier2Context) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.identifier3`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitIdentifier3?: (ctx: Identifier3Context) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.identifier4`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitIdentifier4?: (ctx: Identifier4Context) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.identifier12`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitIdentifier12?: (ctx: Identifier12Context) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.identifier23`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitIdentifier23?: (ctx: Identifier23Context) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.identifier123`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitIdentifier123?: (ctx: Identifier123Context) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.identifier123CNF`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitIdentifier123CNF?: (ctx: Identifier123CNFContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.flowPath`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitFlowPath?: (ctx: FlowPathContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.codeBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCodeBlock?: (ctx: CodeBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.variableBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitVariableBlock?: (ctx: VariableBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.variableDef`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitVariableDef?: (ctx: VariableDefContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.varName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitVarName?: (ctx: VarNameContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.argumentGroups`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitArgumentGroups?: (ctx: ArgumentGroupsContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.argumentGroup`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitArgumentGroup?: (ctx: ArgumentGroupContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.argument`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitArgument?: (ctx: ArgumentContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.varIdentifier`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitVarIdentifier?: (ctx: VarIdentifierContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.intValue`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitIntValue?: (ctx: IntValueContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.floatValue`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitFloatValue?: (ctx: FloatValueContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.varType`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitVarType?: (ctx: VarTypeContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.funApplication`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitFunApplication?: (ctx: FunApplicationContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.commandBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCommandBlock?: (ctx: CommandBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.commandDef`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCommandDef?: (ctx: CommandDefContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.cmdName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCmdName?: (ctx: CmdNameContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.funName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitFunName?: (ctx: FunNameContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.observeBlock`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitObserveBlock?: (ctx: ObserveBlockContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.observeDef`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitObserveDef?: (ctx: ObserveDefContext) => Result;

	/**
	 * Visit a parse tree produced by `dsParser.observeName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitObserveName?: (ctx: ObserveNameContext) => Result;
}

