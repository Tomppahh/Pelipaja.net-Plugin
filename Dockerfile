FROM juksuu/cs2:matchup

ARG CSSAPI_VERSION=1.0.367
ARG METAMOD_VERSION=1401

WORKDIR /root

RUN apt-get update && apt-get install -y wget unzip && rm -rf /var/lib/apt/lists/*

ENV CSSAPI_VERSION=${CSSAPI_VERSION}
ENV METAMOD_VERSION=${METAMOD_VERSION}

COPY matchup.sh /root/matchup.sh
COPY bin/Release/net10.0/MatchUp.dll /tmp/MatchUp.dll
COPY MatchUp.csproj ./MatchUp.csproj
RUN chmod +x /root/matchup.sh
