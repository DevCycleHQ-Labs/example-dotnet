redeploy:
	docker build --no-cache -t hellotogglebot:latest . && \
	kubectl rollout restart deployment hellotogglebot
