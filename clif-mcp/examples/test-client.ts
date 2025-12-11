
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StdioClientTransport } from "@modelcontextprotocol/sdk/client/stdio.js";
import path from "path";

async function run() {
  const transport = new StdioClientTransport({
    command: "node",
    args: ["../dist/index.js"], // Adjusted path for running from examples dir, or adjust as needed
    env: {
        ...process.env as Record<string, string>,
        // CLIF_PATH must be set in env or this script
    }
  });

  const client = new Client(
    {
      name: "test-client",
      version: "1.0.0",
    },
    {
      capabilities: {},
    }
  );

  await client.connect(transport);

  console.log("Connected to MCP server");

  console.log("Listing tools...");
  const tools = await client.listTools();
  console.log("Tools:", tools.tools.map(t => t.name));

  console.log("Calling list_processes...");
  const listResult = await client.callTool({
    name: "list_processes",
    arguments: { detailed: true }
  });
  console.log("list_processes result:", listResult);

  // Example click call
  // const clickResult = await client.callTool({
  //   name: "click",
  //   arguments: { processId: 1234, element: "id=Submit" }
  // });
  // console.log("click result:", clickResult);

  await client.close();
}

run().catch(console.error);
