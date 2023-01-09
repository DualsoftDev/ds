// Generated from ../../../Grammar/g4s/dsParser.g4 by ANTLR 4.9.0-SNAPSHOT


import { ParseTreeListener } from "antlr4ts/tree/ParseTreeListener";

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
 * This interface defines a complete listener for a parse tree produced by
 * `dsParser`.
 */
export interface dsParserListener extends ParseTreeListener {
	/**
	 * Enter a parse tree produced by `dsParser.comment`.
	 * @param ctx the parse tree
	 */
	enterComment?: (ctx: CommentContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.comment`.
	 * @param ctx the parse tree
	 */
	exitComment?: (ctx: CommentContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.system`.
	 * @param ctx the parse tree
	 */
	enterSystem?: (ctx: SystemContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.system`.
	 * @param ctx the parse tree
	 */
	exitSystem?: (ctx: SystemContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.sysHeader`.
	 * @param ctx the parse tree
	 */
	enterSysHeader?: (ctx: SysHeaderContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.sysHeader`.
	 * @param ctx the parse tree
	 */
	exitSysHeader?: (ctx: SysHeaderContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.sysBlock`.
	 * @param ctx the parse tree
	 */
	enterSysBlock?: (ctx: SysBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.sysBlock`.
	 * @param ctx the parse tree
	 */
	exitSysBlock?: (ctx: SysBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.systemName`.
	 * @param ctx the parse tree
	 */
	enterSystemName?: (ctx: SystemNameContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.systemName`.
	 * @param ctx the parse tree
	 */
	exitSystemName?: (ctx: SystemNameContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.ipSpec`.
	 * @param ctx the parse tree
	 */
	enterIpSpec?: (ctx: IpSpecContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.ipSpec`.
	 * @param ctx the parse tree
	 */
	exitIpSpec?: (ctx: IpSpecContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.host`.
	 * @param ctx the parse tree
	 */
	enterHost?: (ctx: HostContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.host`.
	 * @param ctx the parse tree
	 */
	exitHost?: (ctx: HostContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.etcName`.
	 * @param ctx the parse tree
	 */
	enterEtcName?: (ctx: EtcNameContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.etcName`.
	 * @param ctx the parse tree
	 */
	exitEtcName?: (ctx: EtcNameContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.ipv4`.
	 * @param ctx the parse tree
	 */
	enterIpv4?: (ctx: Ipv4Context) => void;
	/**
	 * Exit a parse tree produced by `dsParser.ipv4`.
	 * @param ctx the parse tree
	 */
	exitIpv4?: (ctx: Ipv4Context) => void;

	/**
	 * Enter a parse tree produced by `dsParser.loadDeviceBlock`.
	 * @param ctx the parse tree
	 */
	enterLoadDeviceBlock?: (ctx: LoadDeviceBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.loadDeviceBlock`.
	 * @param ctx the parse tree
	 */
	exitLoadDeviceBlock?: (ctx: LoadDeviceBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.deviceName`.
	 * @param ctx the parse tree
	 */
	enterDeviceName?: (ctx: DeviceNameContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.deviceName`.
	 * @param ctx the parse tree
	 */
	exitDeviceName?: (ctx: DeviceNameContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.fileSpec`.
	 * @param ctx the parse tree
	 */
	enterFileSpec?: (ctx: FileSpecContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.fileSpec`.
	 * @param ctx the parse tree
	 */
	exitFileSpec?: (ctx: FileSpecContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.etcName1`.
	 * @param ctx the parse tree
	 */
	enterEtcName1?: (ctx: EtcName1Context) => void;
	/**
	 * Exit a parse tree produced by `dsParser.etcName1`.
	 * @param ctx the parse tree
	 */
	exitEtcName1?: (ctx: EtcName1Context) => void;

	/**
	 * Enter a parse tree produced by `dsParser.filePath`.
	 * @param ctx the parse tree
	 */
	enterFilePath?: (ctx: FilePathContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.filePath`.
	 * @param ctx the parse tree
	 */
	exitFilePath?: (ctx: FilePathContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.loadExternalSystemBlock`.
	 * @param ctx the parse tree
	 */
	enterLoadExternalSystemBlock?: (ctx: LoadExternalSystemBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.loadExternalSystemBlock`.
	 * @param ctx the parse tree
	 */
	exitLoadExternalSystemBlock?: (ctx: LoadExternalSystemBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.externalSystemName`.
	 * @param ctx the parse tree
	 */
	enterExternalSystemName?: (ctx: ExternalSystemNameContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.externalSystemName`.
	 * @param ctx the parse tree
	 */
	exitExternalSystemName?: (ctx: ExternalSystemNameContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.addressInOut`.
	 * @param ctx the parse tree
	 */
	enterAddressInOut?: (ctx: AddressInOutContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.addressInOut`.
	 * @param ctx the parse tree
	 */
	exitAddressInOut?: (ctx: AddressInOutContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.inAddr`.
	 * @param ctx the parse tree
	 */
	enterInAddr?: (ctx: InAddrContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.inAddr`.
	 * @param ctx the parse tree
	 */
	exitInAddr?: (ctx: InAddrContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.outAddr`.
	 * @param ctx the parse tree
	 */
	enterOutAddr?: (ctx: OutAddrContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.outAddr`.
	 * @param ctx the parse tree
	 */
	exitOutAddr?: (ctx: OutAddrContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.addressItem`.
	 * @param ctx the parse tree
	 */
	enterAddressItem?: (ctx: AddressItemContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.addressItem`.
	 * @param ctx the parse tree
	 */
	exitAddressItem?: (ctx: AddressItemContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.tagAddress`.
	 * @param ctx the parse tree
	 */
	enterTagAddress?: (ctx: TagAddressContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.tagAddress`.
	 * @param ctx the parse tree
	 */
	exitTagAddress?: (ctx: TagAddressContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.funAddress`.
	 * @param ctx the parse tree
	 */
	enterFunAddress?: (ctx: FunAddressContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.funAddress`.
	 * @param ctx the parse tree
	 */
	exitFunAddress?: (ctx: FunAddressContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.propsBlock`.
	 * @param ctx the parse tree
	 */
	enterPropsBlock?: (ctx: PropsBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.propsBlock`.
	 * @param ctx the parse tree
	 */
	exitPropsBlock?: (ctx: PropsBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.safetyBlock`.
	 * @param ctx the parse tree
	 */
	enterSafetyBlock?: (ctx: SafetyBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.safetyBlock`.
	 * @param ctx the parse tree
	 */
	exitSafetyBlock?: (ctx: SafetyBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.safetyDef`.
	 * @param ctx the parse tree
	 */
	enterSafetyDef?: (ctx: SafetyDefContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.safetyDef`.
	 * @param ctx the parse tree
	 */
	exitSafetyDef?: (ctx: SafetyDefContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.safetyKey`.
	 * @param ctx the parse tree
	 */
	enterSafetyKey?: (ctx: SafetyKeyContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.safetyKey`.
	 * @param ctx the parse tree
	 */
	exitSafetyKey?: (ctx: SafetyKeyContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.safetyValues`.
	 * @param ctx the parse tree
	 */
	enterSafetyValues?: (ctx: SafetyValuesContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.safetyValues`.
	 * @param ctx the parse tree
	 */
	exitSafetyValues?: (ctx: SafetyValuesContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.layoutBlock`.
	 * @param ctx the parse tree
	 */
	enterLayoutBlock?: (ctx: LayoutBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.layoutBlock`.
	 * @param ctx the parse tree
	 */
	exitLayoutBlock?: (ctx: LayoutBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.positionDef`.
	 * @param ctx the parse tree
	 */
	enterPositionDef?: (ctx: PositionDefContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.positionDef`.
	 * @param ctx the parse tree
	 */
	exitPositionDef?: (ctx: PositionDefContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.callName`.
	 * @param ctx the parse tree
	 */
	enterCallName?: (ctx: CallNameContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.callName`.
	 * @param ctx the parse tree
	 */
	exitCallName?: (ctx: CallNameContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.xywh`.
	 * @param ctx the parse tree
	 */
	enterXywh?: (ctx: XywhContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.xywh`.
	 * @param ctx the parse tree
	 */
	exitXywh?: (ctx: XywhContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.x`.
	 * @param ctx the parse tree
	 */
	enterX?: (ctx: XContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.x`.
	 * @param ctx the parse tree
	 */
	exitX?: (ctx: XContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.y`.
	 * @param ctx the parse tree
	 */
	enterY?: (ctx: YContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.y`.
	 * @param ctx the parse tree
	 */
	exitY?: (ctx: YContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.w`.
	 * @param ctx the parse tree
	 */
	enterW?: (ctx: WContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.w`.
	 * @param ctx the parse tree
	 */
	exitW?: (ctx: WContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.h`.
	 * @param ctx the parse tree
	 */
	enterH?: (ctx: HContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.h`.
	 * @param ctx the parse tree
	 */
	exitH?: (ctx: HContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.flowBlock`.
	 * @param ctx the parse tree
	 */
	enterFlowBlock?: (ctx: FlowBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.flowBlock`.
	 * @param ctx the parse tree
	 */
	exitFlowBlock?: (ctx: FlowBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.parentingBlock`.
	 * @param ctx the parse tree
	 */
	enterParentingBlock?: (ctx: ParentingBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.parentingBlock`.
	 * @param ctx the parse tree
	 */
	exitParentingBlock?: (ctx: ParentingBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.identifier12Listing`.
	 * @param ctx the parse tree
	 */
	enterIdentifier12Listing?: (ctx: Identifier12ListingContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.identifier12Listing`.
	 * @param ctx the parse tree
	 */
	exitIdentifier12Listing?: (ctx: Identifier12ListingContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.identifier1Listing`.
	 * @param ctx the parse tree
	 */
	enterIdentifier1Listing?: (ctx: Identifier1ListingContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.identifier1Listing`.
	 * @param ctx the parse tree
	 */
	exitIdentifier1Listing?: (ctx: Identifier1ListingContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.identifier2Listing`.
	 * @param ctx the parse tree
	 */
	enterIdentifier2Listing?: (ctx: Identifier2ListingContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.identifier2Listing`.
	 * @param ctx the parse tree
	 */
	exitIdentifier2Listing?: (ctx: Identifier2ListingContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.aliasBlock`.
	 * @param ctx the parse tree
	 */
	enterAliasBlock?: (ctx: AliasBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.aliasBlock`.
	 * @param ctx the parse tree
	 */
	exitAliasBlock?: (ctx: AliasBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.aliasListing`.
	 * @param ctx the parse tree
	 */
	enterAliasListing?: (ctx: AliasListingContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.aliasListing`.
	 * @param ctx the parse tree
	 */
	exitAliasListing?: (ctx: AliasListingContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.aliasDef`.
	 * @param ctx the parse tree
	 */
	enterAliasDef?: (ctx: AliasDefContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.aliasDef`.
	 * @param ctx the parse tree
	 */
	exitAliasDef?: (ctx: AliasDefContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.aliasMnemonic`.
	 * @param ctx the parse tree
	 */
	enterAliasMnemonic?: (ctx: AliasMnemonicContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.aliasMnemonic`.
	 * @param ctx the parse tree
	 */
	exitAliasMnemonic?: (ctx: AliasMnemonicContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.jobBlock`.
	 * @param ctx the parse tree
	 */
	enterJobBlock?: (ctx: JobBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.jobBlock`.
	 * @param ctx the parse tree
	 */
	exitJobBlock?: (ctx: JobBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.callListing`.
	 * @param ctx the parse tree
	 */
	enterCallListing?: (ctx: CallListingContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.callListing`.
	 * @param ctx the parse tree
	 */
	exitCallListing?: (ctx: CallListingContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.jobName`.
	 * @param ctx the parse tree
	 */
	enterJobName?: (ctx: JobNameContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.jobName`.
	 * @param ctx the parse tree
	 */
	exitJobName?: (ctx: JobNameContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.callApiDef`.
	 * @param ctx the parse tree
	 */
	enterCallApiDef?: (ctx: CallApiDefContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.callApiDef`.
	 * @param ctx the parse tree
	 */
	exitCallApiDef?: (ctx: CallApiDefContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.callKey`.
	 * @param ctx the parse tree
	 */
	enterCallKey?: (ctx: CallKeyContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.callKey`.
	 * @param ctx the parse tree
	 */
	exitCallKey?: (ctx: CallKeyContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.interfaceBlock`.
	 * @param ctx the parse tree
	 */
	enterInterfaceBlock?: (ctx: InterfaceBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.interfaceBlock`.
	 * @param ctx the parse tree
	 */
	exitInterfaceBlock?: (ctx: InterfaceBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.interfaceListing`.
	 * @param ctx the parse tree
	 */
	enterInterfaceListing?: (ctx: InterfaceListingContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.interfaceListing`.
	 * @param ctx the parse tree
	 */
	exitInterfaceListing?: (ctx: InterfaceListingContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.interfaceDef`.
	 * @param ctx the parse tree
	 */
	enterInterfaceDef?: (ctx: InterfaceDefContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.interfaceDef`.
	 * @param ctx the parse tree
	 */
	exitInterfaceDef?: (ctx: InterfaceDefContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.interfaceName`.
	 * @param ctx the parse tree
	 */
	enterInterfaceName?: (ctx: InterfaceNameContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.interfaceName`.
	 * @param ctx the parse tree
	 */
	exitInterfaceName?: (ctx: InterfaceNameContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.serPhrase`.
	 * @param ctx the parse tree
	 */
	enterSerPhrase?: (ctx: SerPhraseContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.serPhrase`.
	 * @param ctx the parse tree
	 */
	exitSerPhrase?: (ctx: SerPhraseContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.callComponents`.
	 * @param ctx the parse tree
	 */
	enterCallComponents?: (ctx: CallComponentsContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.callComponents`.
	 * @param ctx the parse tree
	 */
	exitCallComponents?: (ctx: CallComponentsContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.interfaceResetDef`.
	 * @param ctx the parse tree
	 */
	enterInterfaceResetDef?: (ctx: InterfaceResetDefContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.interfaceResetDef`.
	 * @param ctx the parse tree
	 */
	exitInterfaceResetDef?: (ctx: InterfaceResetDefContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.buttonsBlocks`.
	 * @param ctx the parse tree
	 */
	enterButtonsBlocks?: (ctx: ButtonsBlocksContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.buttonsBlocks`.
	 * @param ctx the parse tree
	 */
	exitButtonsBlocks?: (ctx: ButtonsBlocksContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.emergencyButtonBlock`.
	 * @param ctx the parse tree
	 */
	enterEmergencyButtonBlock?: (ctx: EmergencyButtonBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.emergencyButtonBlock`.
	 * @param ctx the parse tree
	 */
	exitEmergencyButtonBlock?: (ctx: EmergencyButtonBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.autoButtonBlock`.
	 * @param ctx the parse tree
	 */
	enterAutoButtonBlock?: (ctx: AutoButtonBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.autoButtonBlock`.
	 * @param ctx the parse tree
	 */
	exitAutoButtonBlock?: (ctx: AutoButtonBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.clearButtonBlock`.
	 * @param ctx the parse tree
	 */
	enterClearButtonBlock?: (ctx: ClearButtonBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.clearButtonBlock`.
	 * @param ctx the parse tree
	 */
	exitClearButtonBlock?: (ctx: ClearButtonBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.manualButtonBlock`.
	 * @param ctx the parse tree
	 */
	enterManualButtonBlock?: (ctx: ManualButtonBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.manualButtonBlock`.
	 * @param ctx the parse tree
	 */
	exitManualButtonBlock?: (ctx: ManualButtonBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.stopButtonBlock`.
	 * @param ctx the parse tree
	 */
	enterStopButtonBlock?: (ctx: StopButtonBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.stopButtonBlock`.
	 * @param ctx the parse tree
	 */
	exitStopButtonBlock?: (ctx: StopButtonBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.runButtonBlock`.
	 * @param ctx the parse tree
	 */
	enterRunButtonBlock?: (ctx: RunButtonBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.runButtonBlock`.
	 * @param ctx the parse tree
	 */
	exitRunButtonBlock?: (ctx: RunButtonBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.dryrunButtonBlock`.
	 * @param ctx the parse tree
	 */
	enterDryrunButtonBlock?: (ctx: DryrunButtonBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.dryrunButtonBlock`.
	 * @param ctx the parse tree
	 */
	exitDryrunButtonBlock?: (ctx: DryrunButtonBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.homeButtonBlock`.
	 * @param ctx the parse tree
	 */
	enterHomeButtonBlock?: (ctx: HomeButtonBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.homeButtonBlock`.
	 * @param ctx the parse tree
	 */
	exitHomeButtonBlock?: (ctx: HomeButtonBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.buttonBlock`.
	 * @param ctx the parse tree
	 */
	enterButtonBlock?: (ctx: ButtonBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.buttonBlock`.
	 * @param ctx the parse tree
	 */
	exitButtonBlock?: (ctx: ButtonBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.buttonDef`.
	 * @param ctx the parse tree
	 */
	enterButtonDef?: (ctx: ButtonDefContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.buttonDef`.
	 * @param ctx the parse tree
	 */
	exitButtonDef?: (ctx: ButtonDefContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.btnNameAddr`.
	 * @param ctx the parse tree
	 */
	enterBtnNameAddr?: (ctx: BtnNameAddrContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.btnNameAddr`.
	 * @param ctx the parse tree
	 */
	exitBtnNameAddr?: (ctx: BtnNameAddrContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.buttonName`.
	 * @param ctx the parse tree
	 */
	enterButtonName?: (ctx: ButtonNameContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.buttonName`.
	 * @param ctx the parse tree
	 */
	exitButtonName?: (ctx: ButtonNameContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.flowName`.
	 * @param ctx the parse tree
	 */
	enterFlowName?: (ctx: FlowNameContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.flowName`.
	 * @param ctx the parse tree
	 */
	exitFlowName?: (ctx: FlowNameContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.lampBlocks`.
	 * @param ctx the parse tree
	 */
	enterLampBlocks?: (ctx: LampBlocksContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.lampBlocks`.
	 * @param ctx the parse tree
	 */
	exitLampBlocks?: (ctx: LampBlocksContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.runLampBlock`.
	 * @param ctx the parse tree
	 */
	enterRunLampBlock?: (ctx: RunLampBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.runLampBlock`.
	 * @param ctx the parse tree
	 */
	exitRunLampBlock?: (ctx: RunLampBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.dryrunLampBlock`.
	 * @param ctx the parse tree
	 */
	enterDryrunLampBlock?: (ctx: DryrunLampBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.dryrunLampBlock`.
	 * @param ctx the parse tree
	 */
	exitDryrunLampBlock?: (ctx: DryrunLampBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.manualLampBlock`.
	 * @param ctx the parse tree
	 */
	enterManualLampBlock?: (ctx: ManualLampBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.manualLampBlock`.
	 * @param ctx the parse tree
	 */
	exitManualLampBlock?: (ctx: ManualLampBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.stopLampBlock`.
	 * @param ctx the parse tree
	 */
	enterStopLampBlock?: (ctx: StopLampBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.stopLampBlock`.
	 * @param ctx the parse tree
	 */
	exitStopLampBlock?: (ctx: StopLampBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.emgLampBlock`.
	 * @param ctx the parse tree
	 */
	enterEmgLampBlock?: (ctx: EmgLampBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.emgLampBlock`.
	 * @param ctx the parse tree
	 */
	exitEmgLampBlock?: (ctx: EmgLampBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.lampBlock`.
	 * @param ctx the parse tree
	 */
	enterLampBlock?: (ctx: LampBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.lampBlock`.
	 * @param ctx the parse tree
	 */
	exitLampBlock?: (ctx: LampBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.lampDef`.
	 * @param ctx the parse tree
	 */
	enterLampDef?: (ctx: LampDefContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.lampDef`.
	 * @param ctx the parse tree
	 */
	exitLampDef?: (ctx: LampDefContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.addrDef`.
	 * @param ctx the parse tree
	 */
	enterAddrDef?: (ctx: AddrDefContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.addrDef`.
	 * @param ctx the parse tree
	 */
	exitAddrDef?: (ctx: AddrDefContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.lampName`.
	 * @param ctx the parse tree
	 */
	enterLampName?: (ctx: LampNameContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.lampName`.
	 * @param ctx the parse tree
	 */
	exitLampName?: (ctx: LampNameContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.causal`.
	 * @param ctx the parse tree
	 */
	enterCausal?: (ctx: CausalContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.causal`.
	 * @param ctx the parse tree
	 */
	exitCausal?: (ctx: CausalContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.causalPhrase`.
	 * @param ctx the parse tree
	 */
	enterCausalPhrase?: (ctx: CausalPhraseContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.causalPhrase`.
	 * @param ctx the parse tree
	 */
	exitCausalPhrase?: (ctx: CausalPhraseContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.causalTokensCNF`.
	 * @param ctx the parse tree
	 */
	enterCausalTokensCNF?: (ctx: CausalTokensCNFContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.causalTokensCNF`.
	 * @param ctx the parse tree
	 */
	exitCausalTokensCNF?: (ctx: CausalTokensCNFContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.causalToken`.
	 * @param ctx the parse tree
	 */
	enterCausalToken?: (ctx: CausalTokenContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.causalToken`.
	 * @param ctx the parse tree
	 */
	exitCausalToken?: (ctx: CausalTokenContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.causalOperator`.
	 * @param ctx the parse tree
	 */
	enterCausalOperator?: (ctx: CausalOperatorContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.causalOperator`.
	 * @param ctx the parse tree
	 */
	exitCausalOperator?: (ctx: CausalOperatorContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.causalOperatorReset`.
	 * @param ctx the parse tree
	 */
	enterCausalOperatorReset?: (ctx: CausalOperatorResetContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.causalOperatorReset`.
	 * @param ctx the parse tree
	 */
	exitCausalOperatorReset?: (ctx: CausalOperatorResetContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.identifier1234`.
	 * @param ctx the parse tree
	 */
	enterIdentifier1234?: (ctx: Identifier1234Context) => void;
	/**
	 * Exit a parse tree produced by `dsParser.identifier1234`.
	 * @param ctx the parse tree
	 */
	exitIdentifier1234?: (ctx: Identifier1234Context) => void;

	/**
	 * Enter a parse tree produced by `dsParser.identifier1`.
	 * @param ctx the parse tree
	 */
	enterIdentifier1?: (ctx: Identifier1Context) => void;
	/**
	 * Exit a parse tree produced by `dsParser.identifier1`.
	 * @param ctx the parse tree
	 */
	exitIdentifier1?: (ctx: Identifier1Context) => void;

	/**
	 * Enter a parse tree produced by `dsParser.identifier2`.
	 * @param ctx the parse tree
	 */
	enterIdentifier2?: (ctx: Identifier2Context) => void;
	/**
	 * Exit a parse tree produced by `dsParser.identifier2`.
	 * @param ctx the parse tree
	 */
	exitIdentifier2?: (ctx: Identifier2Context) => void;

	/**
	 * Enter a parse tree produced by `dsParser.identifier3`.
	 * @param ctx the parse tree
	 */
	enterIdentifier3?: (ctx: Identifier3Context) => void;
	/**
	 * Exit a parse tree produced by `dsParser.identifier3`.
	 * @param ctx the parse tree
	 */
	exitIdentifier3?: (ctx: Identifier3Context) => void;

	/**
	 * Enter a parse tree produced by `dsParser.identifier4`.
	 * @param ctx the parse tree
	 */
	enterIdentifier4?: (ctx: Identifier4Context) => void;
	/**
	 * Exit a parse tree produced by `dsParser.identifier4`.
	 * @param ctx the parse tree
	 */
	exitIdentifier4?: (ctx: Identifier4Context) => void;

	/**
	 * Enter a parse tree produced by `dsParser.identifier12`.
	 * @param ctx the parse tree
	 */
	enterIdentifier12?: (ctx: Identifier12Context) => void;
	/**
	 * Exit a parse tree produced by `dsParser.identifier12`.
	 * @param ctx the parse tree
	 */
	exitIdentifier12?: (ctx: Identifier12Context) => void;

	/**
	 * Enter a parse tree produced by `dsParser.identifier23`.
	 * @param ctx the parse tree
	 */
	enterIdentifier23?: (ctx: Identifier23Context) => void;
	/**
	 * Exit a parse tree produced by `dsParser.identifier23`.
	 * @param ctx the parse tree
	 */
	exitIdentifier23?: (ctx: Identifier23Context) => void;

	/**
	 * Enter a parse tree produced by `dsParser.identifier123`.
	 * @param ctx the parse tree
	 */
	enterIdentifier123?: (ctx: Identifier123Context) => void;
	/**
	 * Exit a parse tree produced by `dsParser.identifier123`.
	 * @param ctx the parse tree
	 */
	exitIdentifier123?: (ctx: Identifier123Context) => void;

	/**
	 * Enter a parse tree produced by `dsParser.identifier123CNF`.
	 * @param ctx the parse tree
	 */
	enterIdentifier123CNF?: (ctx: Identifier123CNFContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.identifier123CNF`.
	 * @param ctx the parse tree
	 */
	exitIdentifier123CNF?: (ctx: Identifier123CNFContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.flowPath`.
	 * @param ctx the parse tree
	 */
	enterFlowPath?: (ctx: FlowPathContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.flowPath`.
	 * @param ctx the parse tree
	 */
	exitFlowPath?: (ctx: FlowPathContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.codeBlock`.
	 * @param ctx the parse tree
	 */
	enterCodeBlock?: (ctx: CodeBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.codeBlock`.
	 * @param ctx the parse tree
	 */
	exitCodeBlock?: (ctx: CodeBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.variableBlock`.
	 * @param ctx the parse tree
	 */
	enterVariableBlock?: (ctx: VariableBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.variableBlock`.
	 * @param ctx the parse tree
	 */
	exitVariableBlock?: (ctx: VariableBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.variableDef`.
	 * @param ctx the parse tree
	 */
	enterVariableDef?: (ctx: VariableDefContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.variableDef`.
	 * @param ctx the parse tree
	 */
	exitVariableDef?: (ctx: VariableDefContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.varName`.
	 * @param ctx the parse tree
	 */
	enterVarName?: (ctx: VarNameContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.varName`.
	 * @param ctx the parse tree
	 */
	exitVarName?: (ctx: VarNameContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.argumentGroups`.
	 * @param ctx the parse tree
	 */
	enterArgumentGroups?: (ctx: ArgumentGroupsContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.argumentGroups`.
	 * @param ctx the parse tree
	 */
	exitArgumentGroups?: (ctx: ArgumentGroupsContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.argumentGroup`.
	 * @param ctx the parse tree
	 */
	enterArgumentGroup?: (ctx: ArgumentGroupContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.argumentGroup`.
	 * @param ctx the parse tree
	 */
	exitArgumentGroup?: (ctx: ArgumentGroupContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.argument`.
	 * @param ctx the parse tree
	 */
	enterArgument?: (ctx: ArgumentContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.argument`.
	 * @param ctx the parse tree
	 */
	exitArgument?: (ctx: ArgumentContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.varIdentifier`.
	 * @param ctx the parse tree
	 */
	enterVarIdentifier?: (ctx: VarIdentifierContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.varIdentifier`.
	 * @param ctx the parse tree
	 */
	exitVarIdentifier?: (ctx: VarIdentifierContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.intValue`.
	 * @param ctx the parse tree
	 */
	enterIntValue?: (ctx: IntValueContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.intValue`.
	 * @param ctx the parse tree
	 */
	exitIntValue?: (ctx: IntValueContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.floatValue`.
	 * @param ctx the parse tree
	 */
	enterFloatValue?: (ctx: FloatValueContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.floatValue`.
	 * @param ctx the parse tree
	 */
	exitFloatValue?: (ctx: FloatValueContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.varType`.
	 * @param ctx the parse tree
	 */
	enterVarType?: (ctx: VarTypeContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.varType`.
	 * @param ctx the parse tree
	 */
	exitVarType?: (ctx: VarTypeContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.funApplication`.
	 * @param ctx the parse tree
	 */
	enterFunApplication?: (ctx: FunApplicationContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.funApplication`.
	 * @param ctx the parse tree
	 */
	exitFunApplication?: (ctx: FunApplicationContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.commandBlock`.
	 * @param ctx the parse tree
	 */
	enterCommandBlock?: (ctx: CommandBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.commandBlock`.
	 * @param ctx the parse tree
	 */
	exitCommandBlock?: (ctx: CommandBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.commandDef`.
	 * @param ctx the parse tree
	 */
	enterCommandDef?: (ctx: CommandDefContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.commandDef`.
	 * @param ctx the parse tree
	 */
	exitCommandDef?: (ctx: CommandDefContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.cmdName`.
	 * @param ctx the parse tree
	 */
	enterCmdName?: (ctx: CmdNameContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.cmdName`.
	 * @param ctx the parse tree
	 */
	exitCmdName?: (ctx: CmdNameContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.funName`.
	 * @param ctx the parse tree
	 */
	enterFunName?: (ctx: FunNameContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.funName`.
	 * @param ctx the parse tree
	 */
	exitFunName?: (ctx: FunNameContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.observeBlock`.
	 * @param ctx the parse tree
	 */
	enterObserveBlock?: (ctx: ObserveBlockContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.observeBlock`.
	 * @param ctx the parse tree
	 */
	exitObserveBlock?: (ctx: ObserveBlockContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.observeDef`.
	 * @param ctx the parse tree
	 */
	enterObserveDef?: (ctx: ObserveDefContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.observeDef`.
	 * @param ctx the parse tree
	 */
	exitObserveDef?: (ctx: ObserveDefContext) => void;

	/**
	 * Enter a parse tree produced by `dsParser.observeName`.
	 * @param ctx the parse tree
	 */
	enterObserveName?: (ctx: ObserveNameContext) => void;
	/**
	 * Exit a parse tree produced by `dsParser.observeName`.
	 * @param ctx the parse tree
	 */
	exitObserveName?: (ctx: ObserveNameContext) => void;
}

