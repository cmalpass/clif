#!/usr/bin/env node
import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ErrorCode,
  ListToolsRequestSchema,
  McpError,
} from "@modelcontextprotocol/sdk/types.js";
import { z } from "zod";
import { execFile } from "child_process";
import { promisify } from "util";
import fs from "fs";
import path from "path";
import { zodToJsonSchema as zodToJsonSchemaOriginal } from "zod-to-json-schema";

const execFileAsync = promisify(execFile);

// Configurable path to clif executable
const CLIF_PATH = process.env.CLIF_PATH || "clif";

class ClifMcpServer {
  private server: Server;

  constructor() {
    this.server = new Server(
      {
        name: "clif-mcp-server",
        version: "0.1.0",
      },
      {
        capabilities: {
          tools: {},
        },
      }
    );

    this.setupRequestHandlers();
  }

  private setupRequestHandlers() {
    this.server.setRequestHandler(ListToolsRequestSchema, async () => ({
      tools: [
        {
          name: "list_processes",
          description: "List all available WPF processes suitable for automation.",
          inputSchema: zodToJsonSchema(
            z.object({
              detailed: z.boolean().optional().describe("Show detailed process information"),
            })
          ),
        },
        {
          name: "click",
          description: "Click on a UI element in a target process.",
          inputSchema: zodToJsonSchema(
            z.object({
              processId: z.number().describe("Target process ID"),
              element: z.string().describe("Element selector (e.g., 'id=MyButton', 'name=Submit')"),
            })
          ),
        },
        {
          name: "type",
          description: "Type text into a UI element.",
          inputSchema: zodToJsonSchema(
            z.object({
              processId: z.number().describe("Target process ID"),
              element: z.string().describe("Element selector"),
              text: z.string().describe("Text to type"),
            })
          ),
        },
        {
          name: "interact",
          description: "Advanced interaction with WPF controls (combobox, listbox, etc.).",
          inputSchema: zodToJsonSchema(
            z.object({
              processId: z.number().describe("Target process ID"),
              element: z.string().describe("Element selector"),
              controlType: z.enum([
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
              action: z.string().describe("Action to perform (e.g., 'select', 'toggle', 'expand')"),
              value: z.string().optional().describe("Value for the action"),
              index: z.number().optional().describe("Index for selection actions"),
            })
          ),
        },
        {
          name: "tree",
          description: "Display or search the automation element tree.",
          inputSchema: zodToJsonSchema(
            z.object({
              process: z.union([z.string(), z.number()]).describe("Process name or ID"),
              depth: z.number().optional().describe("Maximum tree depth (default: 10)"),
              search: z.string().optional().describe("Search criteria"),
              enabledOnly: z.boolean().optional().describe("Show only enabled elements"),
              visibleOnly: z.boolean().optional().describe("Show only visible elements"),
            })
          ),
        },
        {
          name: "script",
          description: "Execute a CLIF automation script from a file.",
          inputSchema: zodToJsonSchema(
            z.object({
              scriptPath: z.string().describe("Path to the JSON script file"),
              processId: z.number().optional().describe("Target process ID override"),
            })
          ),
        },
      ],
    }));

    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      try {
        switch (request.params.name) {
          case "list_processes": {
            const args = request.params.arguments as any;
            return await this.runClifCommand("list-processes", {
              "--detailed": args.detailed,
              "--format": "json" // Force JSON for easier parsing if needed, but plain text is fine too
            });
          }
          case "click": {
            const args = request.params.arguments as any;
            return await this.runClifCommand("click", {
              "--process-id": args.processId,
              "--element": args.element,
            });
          }
          case "type": {
            const args = request.params.arguments as any;
            return await this.runClifCommand("type", {
              "--process-id": args.processId,
              "--element": args.element,
              "--text": args.text,
            });
          }
          case "interact": {
            const args = request.params.arguments as any;
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
            const args = request.params.arguments as any;
            const processArg = args.process.toString();
            // tree command uses positional arg for process
            const options: Record<string, any> = {
                "--depth": args.depth,
                "--search": args.search,
            };
            if (args.enabledOnly) options["--enabled-only"] = true;
            if (args.visibleOnly) options["--visible-only"] = true;

            return await this.runClifCommand("tree", options, [processArg]);
          }
          case "script": {
             const args = request.params.arguments as any;
             const options: Record<string, any> = {};
             if (args.processId) options["--process-id"] = args.processId;
             return await this.runClifCommand("script", options, [args.scriptPath]);
          }

          default:
            throw new McpError(
              ErrorCode.MethodNotFound,
              `Unknown tool: ${request.params.name}`
            );
        }
      } catch (error: any) {
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

  private async runClifCommand(
    command: string,
    options: Record<string, any> = {},
    positionalArgs: string[] = []
  ) {
    // Construct command arguments array
    const args: string[] = [command];

    // Add positional arguments first
    args.push(...positionalArgs);

    // Add options
    for (const [key, value] of Object.entries(options)) {
      if (value === undefined || value === null || value === false) continue;

      args.push(key);
      if (value !== true) {
        args.push(value.toString());
      }
    }

    // Execute
    try {
        const { stdout, stderr } = await execFileAsync(CLIF_PATH, args);
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
    } catch (e: any) {
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

  private async findSessionImages(output: string) {
    // Look for "Session started: SESSION_ID" in output
    // Example: [2025-10-25 14:30:15] INFO: Session started: INTERACT_COMBOBOX_143015
    const sessionMatch = output.match(/Session started: ([^\s\r\n]+)/);
    if (!sessionMatch) return [];

    const sessionId = sessionMatch[1];

    // We need to look in the 'sessions' directory relative to CWD
    const sessionsDir = path.resolve(process.cwd(), "sessions");
    if (!fs.existsSync(sessionsDir)) return [];

    // Find folder that contains the session ID
    const entries = await fs.promises.readdir(sessionsDir, { withFileTypes: true });

    // The log ID might be just the prefix or the full name.
    // Let's look for exact match or contains.
    // README says: ./sessions/SESSION_ID_TIMESTAMP/
    // And log says: Session started: INTERACT_COMBOBOX_143015
    // The timestamp in the log "143015" matches the suffix of the ID.

    const sessionDirName = entries.find(e => e.isDirectory() && e.name.includes(sessionId))?.name;

    if (!sessionDirName) return [];

    const fullSessionDir = path.join(sessionsDir, sessionDirName);
    const files = await fs.promises.readdir(fullSessionDir);

    // Filter for pngs
    const pngs = files.filter(f => f.endsWith(".png"));

    const images = [];
    for (const png of pngs) {
        const imagePath = path.join(fullSessionDir, png);
        const imageBuffer = await fs.promises.readFile(imagePath);
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
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    console.error("CLIF MCP Server running on stdio");
  }
}

function zodToJsonSchema(schema: any): any {
    return zodToJsonSchemaOriginal(schema, { target: "jsonSchema7" });
}

const server = new ClifMcpServer();
server.run().catch(console.error);
