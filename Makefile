docker-up:
	docker compose --env-file example.env up -d

migrations-add:
	dotnet ef migrations --project AuthAPI add $(NAME) --output-dir ./Data/Migrations

migrations-update:
	dotnet ef --project AuthAPI database update