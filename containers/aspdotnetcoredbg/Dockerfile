FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-stretch-slim-arm32v7

# add to the base image the components required to support debugger
RUN apt-get update && apt-get install -y \
    unzip \
    curl \
    openssh-server \
    rsync \
    && apt-get clean && apt-get autoremove \
    && rm -rf /var/lib/apt/lists/*

# configure ssh server to allow login as root (uid 0 in the container)
RUN echo "PermitRootLogin without-password" >> /etc/ssh/sshd_config
RUN mkdir /run/sshd

# add RSA key to the container enabling root login without password
COPY id_rsa.pub /root/.ssh/authorized_keys

# install visual studio debugger
RUN curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l ~/vsdbg

# start sshd server at boot
CMD ["/usr/sbin/sshd", "-D"]

# SSH
EXPOSE 22
EXPOSE 5000
EXPOSE 5001
