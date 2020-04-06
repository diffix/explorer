 #!/usr/bin/env bash

docker run -it --rm  -e AIRCLOAK_API_KEY -e AIRCLOAK_API_URL="$1" -p 5005:80 explorer