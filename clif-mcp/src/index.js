#!/usr/bin/env node
"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const index_js_1 = require("@modelcontextprotocol/sdk/server/index.js");
const stdio_js_1 = require("@modelcontextprotocol/sdk/server/stdio.js");
const types_js_1 = require("@modelcontextprotocol/sdk/types.js");
const zod_1 = require("zod");
const child_process_1 = require("child_process");
const util_1 = require("util");
const fs_1 = __importDefault(require("fs"));
const path_1 = __importDefault(require("path"));
const zod_to_json_schema_1 = require("zod-to-json-schema");
const execAsync = (0, util_1.promisify)(child_process_1.exec);
// Configurable path to clif executable
const CLIF_PATH = process.env.CLIF_PATH || "clif";
class ClifMcpServer {
    server;
    constructor() {
        this.server = new index_js_1.Server({
            name: "clif-mcp-server",
            version: "0.1.0",
        }, {
            capabilities: {
                tools: {},
            },
        });
        this.setupRequestHandlers();
    }
    setupRequestHandlers() {
        this.server.setRequestHandler(types_js_1.ListToolsRequestSchema, async () => ({
            tools: [
                {
                    name: "list_processes",
                    description: "List all available WPF processes suitable for automation.",
                    inputSchema: zodToJsonSchema(zod_1.z.object({
                        detailed: zod_1.z.boolean().optional().describe("Show detailed process information"),
                    })),
                },
                {
                    name: "click",
                    description: "Click on a UI element in a target process.",
                    inputSchema: zodToJsonSchema(zod_1.z.object({
                        processId: zod_1.z.number().describe("Target process ID"),
                        element: zod_1.z.string().describe("Element selector (e.g., 'id=MyButton', 'name=Submit')"),
                    })),
                },
                {
                    name: "type",
                    description: "Type text into a UI element.",
                    inputSchema: zodToJsonSchema(zod_1.z.object({
                        processId: zod_1.z.number().describe("Target process ID"),
                        element: zod_1.z.string().describe("Element selector"),
                        text: zod_1.z.string().describe("Text to type"),
                    })),
                },
                {
                    name: "interact",
                    description: "Advanced interaction with WPF controls (combobox, listbox, etc.).",
                    inputSchema: zodToJsonSchema(zod_1.z.object({
                        processId: zod_1.z.number().describe("Target process ID"),
                        element: zod_1.z.string().describe("Element selector"),
                        controlType: zod_1.z.enum([
                            "combobox",
                            "listbox",
                            "checkbox",
                            "radiobutton",
                            "slider",
                            "tab",
                            "tree",
                            "datepicker",
                            "calendar",
                            "expander",
                            "datagrid",
                            "menu",
                            "togglebutton",
                        ]).describe("Type of control to interact with"),
                        action: zod_1.z.string().describe("Action to perform (e.g., 'select', 'toggle', 'expand')"),
                        value: zod_1.z.string().optional().describe("Value for the action"),
                        index: zod_1.z.number().optional().describe("Index for selection actions"),
                    })),
                },
                {
                    name: "tree",
                    description: "Display or search the automation element tree.",
                    inputSchema: zodToJsonSchema(zod_1.z.object({
                        process: zod_1.z.union([zod_1.z.string(), zod_1.z.number()]).describe("Process name or ID"),
                        depth: zod_1.z.number().optional().describe("Maximum tree depth (default: 10)"),
                        search: zod_1.z.string().optional().describe("Search criteria"),
                        enabledOnly: zod_1.z.boolean().optional().describe("Show only enabled elements"),
                        visibleOnly: zod_1.z.boolean().optional().describe("Show only visible elements"),
                    })),
                },
                {
                    name: "script",
                    description: "Execute a CLIF automation script from a file.",
                    inputSchema: zodToJsonSchema(zod_1.z.object({
                        scriptPath: zod_1.z.string().describe("Path to the JSON script file"),
                        processId: zod_1.z.number().optional().describe("Target process ID override"),
                    })),
                },
            ],
        }));
        this.server.setRequestHandler(types_js_1.CallToolRequestSchema, async (request) => {
            try {
                switch (request.params.name) {
                    case "list_processes": {
                        const args = request.params.arguments;
                        return await this.runClifCommand("list-processes", {
                            "--detailed": args.detailed,
                            "--format": "json" // Force JSON for easier parsing if needed, but plain text is fine too
                        });
                    }
                    case "click": {
                        const args = request.params.arguments;
                        return await this.runClifCommand("click", {
                            "--process-id": args.processId,
                            "--element": args.element,
                        });
                    }
                    case "type": {
                        const args = request.params.arguments;
                        return await this.runClifCommand("type", {
                            "--process-id": args.processId,
                            "--element": args.element,
                            "--text": args.text,
                        });
                    }
                    case "interact": {
                        const args = request.params.arguments;
                        return await this.runClifCommand("interact", {
                            "--process-id": args.processId,
                            "--element": args.element,
                            "--control-type": args.controlType,
                            "--action": args.action,
                            "--value": args.value,
                            "--index": args.index,
                        });
                    }
                    case "tree": {
                        const args = request.params.arguments;
                        const processArg = args.process.toString();
                        // tree command uses positional arg for process
                        const options = {
                            "--depth": args.depth,
                            "--search": args.search,
                        };
                        if (args.enabledOnly)
                            options["--enabled-only"] = true;
                        if (args.visibleOnly)
                            options["--visible-only"] = true;
                        return await this.runClifCommand("tree", options, [processArg]);
                    }
                    case "script": {
                        const args = request.params.arguments;
                        const options = {};
                        if (args.processId)
                            options["--process-id"] = args.processId;
                        return await this.runClifCommand("script", options, [args.scriptPath]);
                    }
                    default:
                        throw new types_js_1.McpError(types_js_1.ErrorCode.MethodNotFound, `Unknown tool: ${request.params.name}`);
                }
            }
            catch (error) {
                return {
                    content: [
                        {
                            type: "text",
                            text: `Error executing command: ${error.message}`,
                        },
                    ],
                    isError: true,
                };
            }
        });
    }
    async runClifCommand(command, options = {}, positionalArgs = []) {
        // Construct command string
        const optionArgs = Object.entries(options)
            .filter(([_, value]) => value !== undefined && value !== null && value !== false)
            .map(([key, value]) => {
            if (value === true)
                return key;
            return `${key} "${value}"`; // Quote values to be safe
        });
        const fullCommand = `${CLIF_PATH} ${command} ${positionalArgs.map(a => `"${a}"`).join(" ")} ${optionArgs.join(" ")}`;
        // Execute
        try {
            const { stdout, stderr } = await execAsync(fullCommand);
            const output = stdout + (stderr ? `\nSTDERR:\n${stderr}` : "");
            // Find session images
            const images = await this.findSessionImages(output);
            return {
                content: [
                    {
                        type: "text",
                        text: output,
                    },
                    ...images
                ]
            };
        }
        catch (e) {
            // Even if it fails, clif might have output useful info
            return {
                content: [
                    {
                        type: "text",
                        text: `Command failed: ${e.message}\nSTDOUT:\n${e.stdout}\nSTDERR:\n${e.stderr}`,
                    },
                ],
                isError: true,
            };
        }
    }
    async findSessionImages(output) {
        // Look for "Session started: SESSION_ID" in output
        // Example: [2025-10-25 14:30:15] INFO: Session started: INTERACT_COMBOBOX_143015
        const sessionMatch = output.match(/Session started: ([^\s\r\n]+)/);
        if (!sessionMatch)
            return [];
        const sessionId = sessionMatch[1];
        // We need to look in the 'sessions' directory relative to CWD
        const sessionsDir = path_1.default.resolve(process.cwd(), "sessions");
        if (!fs_1.default.existsSync(sessionsDir))
            return [];
        // Find folder that contains the session ID
        const entries = await fs_1.default.promises.readdir(sessionsDir, { withFileTypes: true });
        // The log ID might be just the prefix or the full name.
        // Let's look for exact match or contains.
        // README says: ./sessions/SESSION_ID_TIMESTAMP/
        // And log says: Session started: INTERACT_COMBOBOX_143015
        // The timestamp in the log "143015" matches the suffix of the ID.
        const sessionDirName = entries.find(e => e.isDirectory() && e.name.includes(sessionId))?.name;
        if (!sessionDirName)
            return [];
        const fullSessionDir = path_1.default.join(sessionsDir, sessionDirName);
        const files = await fs_1.default.promises.readdir(fullSessionDir);
        // Filter for pngs
        const pngs = files.filter(f => f.endsWith(".png"));
        const images = [];
        for (const png of pngs) {
            const imagePath = path_1.default.join(fullSessionDir, png);
            const imageBuffer = await fs_1.default.promises.readFile(imagePath);
            const base64 = imageBuffer.toString("base64");
            images.push({
                type: "image",
                data: base64,
                mimeType: "image/png",
                annotations: {
                    title: png
                }
            });
        }
        return images;
    }
    async run() {
        const transport = new stdio_js_1.StdioServerTransport();
        await this.server.connect(transport);
        console.error("CLIF MCP Server running on stdio");
    }
}
function zodToJsonSchema(schema) {
    return (0, zod_to_json_schema_1.zodToJsonSchema)(schema, { target: "jsonSchema7" });
}
const server = new ClifMcpServer();
server.run().catch(console.error);
//# sourceMappingURL=index.js.map