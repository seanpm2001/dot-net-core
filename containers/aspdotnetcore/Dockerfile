FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-stretch-slim-arm32v7

ARG APPNAME
ENV ENVAPPNAME ${APPNAME}

# this dockerfile must be built from the folder where the application has been published
COPY . /app

WORKDIR /app

# start application at boot
CMD /usr/bin/dotnet ${ENVAPPNAME}

# HTTP
EXPOSE 5000