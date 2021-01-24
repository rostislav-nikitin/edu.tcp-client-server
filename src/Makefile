build:
	dotnet build
run-server:
	cd ./server && dotnet run --project ./server.csproj --ip "127.0.0.1" --port 7777
run-client:
	cd ./client && dotnet run --project ./client.csproj --ip "127.0.0.1" --port 7777 --data "GET /index.html"
run-server-ssl:
	cd ./server && dotnet run --project ./server.csproj --ip "127.0.0.1" --port 7778 --ssl true
run-client-ssl:
	cd ./client && dotnet run --project ./client.csproj --ip "127.0.0.1" --port 7778 --ssl true --ssl-host "localhost" --data "GET /index.html"
