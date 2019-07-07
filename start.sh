    
## docker 部署服务端
cd /data/docker/forward; \
docker stop forward.server; docker rm forward.server; \
docker rmi forward.server:1.0; \
docker build -t forward.server:1.0 . ;\
docker run -d -p 1389:1389 -p 5000:5000 -p 8020:8020 --restart always --name forward.server \
-v /data/docker/forward/logs:/app/logs \
forward.server:1.0

## docker 部署客户端
cd /data/docker/forward.client; \
docker stop forward.client; docker rm forward.client; \
docker rmi forward.client:1.0; \
docker build -t forward.client:1.0 . ;\
docker run -d --restart always --name forward.client \
-v /data/docker/forward.client/logs:/app/logs \
forward.client:1.0