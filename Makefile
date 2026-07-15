# for update docker-compose and .env
docker-up:
	docker compose up -d
	
# for update Dockerfile and C# file 
docker-up-build:
	docker compose up -d --build

# build docker image
docker-build:
	docker build -t dotnet-auth-api .

migrations-add:
	dotnet ef migrations --project AuthAPI add $(NAME) --output-dir ./Data/Migrations

migrations-update:
	dotnet ef --project AuthAPI database update
	
gh-run:
	gh workflow run ci.yml

# Test CI/CD
act:
	act --secret-file .secrets