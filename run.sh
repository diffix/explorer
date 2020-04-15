 #!/usr/bin/env bash

docker run -it --rm  -e AIRCLOAK_API_URL="$1" -p $2:80 explorer