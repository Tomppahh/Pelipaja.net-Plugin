FROM juksuu/cs2:matchup

ARG CSSAPI_VERSION=1.0.371
ARG METAMOD_VERSION=

WORKDIR /root

RUN apt-get update && apt-get install -y wget unzip && rm -rf /var/lib/apt/lists/*

ENV CSSAPI_VERSION=${CSSAPI_VERSION}
ENV METAMOD_VERSION=${METAMOD_VERSION}

COPY matchup.sh /root/matchup.sh
COPY bin/Release/net10.0/MatchUp.dll /tmp/MatchUp.dll
RUN chmod +x /root/matchup.sh
