# Run Backend Services

Start **AggregatorService** (BFF) together with **VocabularyService** and **authorization-module**.

## Invocation

Use `/run-services` when the user wants to run the backend services or start the BFF stack.

## What to Do

Run the project script that starts all three services in separate console windows:

- From repo root: **`npm run services`**  
  or  
- **`powershell -ExecutionPolicy Bypass -File ./run-bff-with-deps.ps1`**

Started endpoints:

- VocabularyService — http://localhost:5117  
- authorization-module — http://localhost:5027  
- AggregatorService (BFF) — http://localhost:5206  

Execute the command in the terminal (workspace root). Each service opens in its own window; the user stops them by closing those windows or Ctrl+C in each.
