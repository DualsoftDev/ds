export * from './dsLexer';
export * from './dsParser';
export * from './dsListener';
export * from './clientParser';
export * from './allVisitor';
export * from './parserUtil';
export * from './preprocessorImpl';


// logger : https://github.com/log4js-node/log4js-node/issues/1009
import {configure, getLogger} from 'log4js';
export let logger = getLogger();
logger.level = 'debug';
logger.debug('logger initialized');
