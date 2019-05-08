# .NET Core development and debugging on Torizon using Visual Studio Code

Those instructions have been tested on Linux (Ubuntu 18.04) and Windows 10.  
On Windows 10 you need to perform some extra configuration steps to allow the required tools to work, those Windows-specific steps will be described in the instructions when required.
If you are not familiar about the conventions we use in the documentation, please check our [online reference](https://developer.toradex.com/knowledge-base/typographic-conventions-for-torizon-documentation)

## Prerequisites

You need to install the following applications on your development machine:
- [Docker](https://www.docker.com)
- [Visual Studio Code](https://code.visualstudio.com/)
- [C# extension for Visual Studio Code](https://github.com/OmniSharp/omnisharp-vscode)
- [.NET Core SDK v3.0](https://dotnet.microsoft.com/download/dotnet-core/3.0)

### Windows 10

On windows you also have to enable the Windows Subsystem for Linux, [you can follow instructions provided by Microsoft](https://docs.microsoft.com/en-us/windows/wsl/install-win10).  

## Configuring the target device

On the target device you should just install a Torizon image with docker support. Your device must be connected to the local network.
Since many commands are executing over ssh, it may be a good idea to enable ssh connection to the target without having to input a password at every connection.  
To do this you should start a terminal (linux) or bash.exe (Windows 10) and run the following commands

```
$ ls $HOME/.ssh
```

If you already have files named id_rsa and id_rsa.pub you can skip those steps, otherwise you have to create your security keys to be able to use them for ssh connections:

```
$ ssh-keygen
Generating public/private rsa key pair.
Enter file in which to save the key (/root/.ssh/id_rsa):
Enter passphrase (empty for no passphrase):
Enter same passphrase again:
Your identification has been saved in /root/.ssh/id_rsa.
Your public key has been saved in /root/.ssh/id_rsa.pub.
The key fingerprint is:
...
The key's randomart image is:
...
```

Now you can copy the keys to the target device and enable ssh login without password. In this sample we are using 192.168.1.114 as the device ip address, please replace it with the actual address/hostname of your device. Default password for the torizon user is "torizon".  Please notice that commands after the "#" prompt will be executed on the target device via ssh.

```
$ scp $HOME/.ssh/id_rsa.pub torizon@192.168.1.114:/home/torizon
torizon@192.168.1.114's password:
id_rsa.pub                                                                            100%  396     0.4KB/s   00:00
$ ssh torizon@192.168.1.114
torizon@192.168.1.114's password:
Last login: Thu May  2 16:09:13 2019 from 192.168.1.3
# cat id_rsa.pub >> .ssh/authorized_keys
# rm id_rsa.pub
# exit
logout
Connection to 192.168.1.114 closed.
# ssh torizon@192.168.1.114
Last login: Fri May  3 09:35:43 2019 from 192.168.1.2
```

If everything works as expected, the second time you tried to connect to the device you should have been able to do it without typing the password.

## creating a new dotnet core project

To create a new project you should have the dot net core SDK v 3.0 installed on your development machine and dotnet (dotnet.exe on Windows 10) in your system path.
Open a terminal/command prompt, create a new folder, cd into it and create a new dotnet application.

```
mkdir mytestapp
cd mytestapp
dotnet new console
code .
```

This will create a new .NET Core application and start Visual Studio Code in that same folder.  

### Configure Visual Studio Code

First time you open a .NET/C# project in Visual Studio Code may take a few minutes because the editor will download all required extension, libraries and componets.
Then you should be able to browse the contents of your folder.  
You should have a .vscode folder with a couple of json files inside it (tasks.json and lauch.json, we will talk about those later). Create a file named settings.json and add the following configuration parameters:

```json
{
    "toradexdotnetcore.targetSSHPort": "8023",
    "toradexdotnetcore.targetDevice": "192.168.1.114",
    "toradexdotnetcore.SSHkey": "../containers/dotnetcoredbg/id_rsa",
    "toradexdotnetcore.containersTemplatePath": "../containers",
    "toradexdotnetcore.maindll": "dotnetcoreapp.dll",
    "toradexdotnetcore.prjname": "dotnetcoreapp.csproj"
}
```

of course replace 192.168.1114 with the IP address of your target device and dotnetcoreapp.* with the name of your current project. You will also need to build some containers (process is described in next chapter, if you keep the containers folder from this repo in the parent folder of your dot net core app you can leave the path in containerTemplatePath as it is, otherwise you should point it to the right folder).

You should also add some tasks to tasks.json and some debug configurations to launch.json.  
If you don't have any custom task/configuration you can just copy the files from our sample (dotnetcoreapp folder).

tasks.json :
 
```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build",
                "${workspaceFolder}/${config:toradexdotnetcore.prjname}"
            ],
            "problemMatcher": "$tsc"
        },
        {
            "label": "publish",
            "command": "dotnet",
            "type": "process",
            "args": [
                "publish",
                "-r",
                "linux-arm",                
                "-o",
                "${workspaceFolder}/bin/app",
                "${workspaceFolder}/${config:toradexdotnetcore.prjname}"
            ],
            "problemMatcher": "$tsc",
            "dependsOn": [
                "build"
            ]
        },
        {
            "label": "deploydebugapp",
            "linux": {
                "command": "ssh torizon@${config:toradexdotnetcore.targetDevice}  'mkdir -p /home/torizon/app' && rsync -avz ${workspaceFolder}/bin/app torizon@${config:toradexdotnetcore.targetDevice}:/home/torizon/",
            },
            "windows": {
                "options": {
                    "cwd": "${workspaceFolder}"
                },
                "command": "bash.exe -c \"ssh torizon@${config:toradexdotnetcore.targetDevice}  'mkdir -p /home/torizon/app' && rsync -avz ./bin/app torizon@${config:toradexdotnetcore.targetDevice}:/home/torizon/\"",
            },
            "type": "shell",
            "problemMatcher": [],
            "dependsOn": [
                "publish"
            ]
        },
        {
            "label": "restartdebugcontainer",
            "linux": {
                "command": "ssh torizon@${config:toradexdotnetcore.targetDevice} 'docker stop ${workspaceFolderBasename}-dbg ; docker run -d --rm --name ${workspaceFolderBasename}-dbg -p ${config:toradexdotnetcore.targetSSHPort}:22 -v $(pwd)/app:/app dotnetcoredbg:latest'",
            },
            "windows": {
                "command": "bash.exe -c \"ssh torizon@${config:toradexdotnetcore.targetDevice} 'docker stop ${workspaceFolderBasename}-dbg ; docker run -d --rm --name ${workspaceFolderBasename}-dbg -p ${config:toradexdotnetcore.targetSSHPort}:22 -v $(pwd)/app:/app dotnetcoredbg:latest'\"",
            },
            "type": "shell",
            "args": [],
            "problemMatcher": [],
            "dependsOn": [
                "deploydebugapp"
            ]
        },
        {
            "label": "waitforsystemready",
            "linux": {
                "command": "while ! nc -z ${config:toradexdotnetcore.targetDevice} ${config:toradexdotnetcore.targetSSHPort}; do sleep 1; done",
            },
            "windows": {
                "command": "bash.exe -c \"while ! nc -z ${config:toradexdotnetcore.targetDevice} ${config:toradexdotnetcore.targetSSHPort}; do sleep 1; done\"",
            },
            "type": "shell",
            "args": [],
            "problemMatcher": [],
            "dependsOn": [
                "restartdebugcontainer"
            ]
        },
        {
            "label": "prepareuserkey",
            "windows": {
                "command": "bash.exe -c \"cp ${config:toradexdotnetcore.SSHkey} $HOME/${workspaceFolderBasename}-containerkey && chmod 600 $HOME/${workspaceFolderBasename}-containerkey\"",
            },
            "type": "shell",
            "args": [],
            "problemMatcher": [],
            "dependsOn": [
                "waitforsystemready"
            ]
        },
        {
            "label": "builddebugcontainer",
            "linux": {
                "options": {
                    "env": {
                        "templatepath": "${workspaceFolder}/${config:toradexdotnetcore.containersTemplatePath}"
                    }
                },
                "command": "cd $templatepath/dotnetcoredbg ; docker build  -t dotnetcoredbg:latest .; cd -",
            },
            "windows": {
                "options": {
                    "env": {
                        "templatepath": "${workspaceFolder}\\${config:toradexdotnetcore.containersTemplatePath}"
                    }
                },
                "command": "pushd . && cd %templatepath%\\dotnetcoredbg && docker build  -t dotnetcoredbg:latest . && popd",
            },
            "type": "shell",
            "args": [],
            "problemMatcher": [],
            "dependsOn": []
        },
        {
            "label": "deploydebugcontainer",
            "linux": {
                "command": "docker save dotnetcoredbg:latest | ssh torizon@${config:toradexdotnetcore.targetDevice} 'docker load'",
            },
            "windows": {
                "command": "docker save dotnetcoredbg:latest | bash.exe -c \"ssh torizon@${config:toradexdotnetcore.targetDevice} 'docker load'\"",
            },
            "type": "shell",
            "args": [],
            "problemMatcher": [],
            "dependsOn": [
                "builddebugcontainer"
            ]
        },
        {
            "label": "buildreleasecontainer",
            "linux": {
                "options": {
                    "env": {
                        "templatepath": "${workspaceFolder}/${config:toradexdotnetcore.containersTemplatePath}"
                    }
                },
                "command": "cd ${workspaceFolder}/bin/app ; docker build -t dotnetcore-${workspaceFolderBasename}:latest --build-arg APPNAME=${config:toradexdotnetcore.maindll} -f $templatepath/dotnetcore/Dockerfile . ; cd -",
            },
            "windows": {
                "options": {
                    "env": {
                        "templatepath": "${workspaceFolder}\\${config:toradexdotnetcore.containersTemplatePath}"
                    }
                },
                "command": "pushd . && cd ${workspaceFolder}\\bin\\app && docker build -t dotnetcore-${workspaceFolderBasename}:latest --build-arg APPNAME=${config:toradexdotnetcore.maindll} -f %templatepath%/dotnetcore/Dockerfile . && popd",
            },
            "type": "shell",
            "args": [],
            "problemMatcher": [],
            "dependsOn": [
                "publish"
            ]
        },
        {
            "label": "deployreleasecontainer",
            "linux": {
                "command": "docker save dotnetcore-${workspaceFolderBasename}:latest | ssh torizon@${config:toradexdotnetcore.targetDevice} 'docker load'",
            },
            "windows": {
                "command": "docker save dotnetcore-${workspaceFolderBasename}:latest | bash.exe -c \"ssh torizon@${config:toradexdotnetcore.targetDevice} 'docker load'\"",
            },
            "type": "shell",
            "args": [],
            "problemMatcher": [],
            "dependsOn": [
                "buildreleasecontainer"
            ]
        },
        {
            "label": "runreleasecontainer",
            "command": "ssh torizon@${config:toradexdotnetcore.targetDevice} 'docker stop ${workspaceFolderBasename} ; docker run -d --rm --name ${workspaceFolderBasename} dotnetcore-${workspaceFolderBasename}:latest'",
            "type": "shell",
            "args": [],
            "problemMatcher": [],
            "dependsOn": [
                "deployreleasecontainer"
            ]
        },
        {
            "label": "watch",
            "command": "dotnet",
            "type": "process",
            "args": [
                "watch",
                "run",
                "${workspaceFolder}/dotnetcoreapp.csproj"
            ],
            "problemMatcher": "$tsc"
        }
    ]
}
```

launch.json :
 
```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Core Launch (console)",
            "type": "coreclr",
            "request": "launch",
            "program": "/usr/bin/dotnet",
            "args": [
                "/app/${config:toradexdotnetcore.maindll}"
            ],
            "cwd": "/app",
            "stopAtEntry": true,
             "console": "integratedTerminal",
            "linux": {
                "preLaunchTask": "waitforsystemready",
                "pipeTransport": {
                    "pipeCwd": "${workspaceFolder}",
                    "pipeProgram": "/usr/bin/ssh",
                    "pipeArgs": [
                        "-p",
                        "${config:toradexdotnetcore.targetSSHPort}",
                        "-i",
                        "${config:toradexdotnetcore.SSHkey}",
                        "-oStrictHostKeyChecking=no",
                        "root@${config:toradexdotnetcore.targetDevice}"
                    ],
                    "debuggerPath": "/root/vsdbg/vsdbg"
                }
            },
            "windows": {
                "preLaunchTask": "prepareuserkey",
                "pipeTransport": {
                    "pipeCwd": "${workspaceFolder}",
                    "pipeProgram": "ssh",
                    "pipeArgs": [
                        "-p",
                        "${config:toradexdotnetcore.targetSSHPort}",
                        "-i",
                        "$HOME/${workspaceFolderBasename}-containerkey",
                        "-oStrictHostKeyChecking=no",
                        "root@${config:toradexdotnetcore.targetDevice}"
                    ],
                    "debuggerPath": "/root/vsdbg/vsdbg"
                }
            },
        }
    ]
}
```

After you made to changes you shoud close and re-open Visual Studio Code to ensure that the new configuration settings are applied.

### building and deploying the debug container

Before you can start developing and debugging your application you should build the container that will be used to run and debug it.  
To build the container, perform following steps:

- Press **ctrl+P** to open the command palette
- type **task builddebugcontainer** and press enter
- build will start, it may take a few minutes and at the end you should see the following message:  

    ```
    Successfully built XXXXXXXX
    Successfully tagged dotnetcoredbg:latest
    ```

- Press **ctrl+P** again to re-open command palette
- type **task deploydebugcontainer** and press enter
- the system will re-run build (it will be very fast, because container image is alread up to date after previous step) and then deploy the container to the target device.  This will be done over local network and should take a couple of minutes.
- If the deployment has been successful you should see the following message (the first sentence will appear only if you already deployed the container to that device in the past):

    ```
    > Executing task: docker save dotnetcoredbg:latest | ssh torizon@192.168.1.114 'docker load' <

    The image dotnetcoredbg:latest already exists, renaming the old one with ID sha256:c0c31bd58adc9cb8e3ee69425b620b29e699a59151ce6689fe09d604a825868b to empty string
    Loaded image: dotnetcoredbg:latest
    ```

Once you deployed the container once you will probably not need to do it again because the application and all its dependencies will be kept separated from the container used for debugging, this will make debug-fix-debug cycle much faster.

### build and run your application

To run your application on the target check that the ".NET Core Launch (console)" entry is selected in the debug window and press F5 to start debugging.  
Visual Studio Code will build, publish and deploy your application to the target, restart debug container (to ensure that execution behaviour won't be influenced by previous runs) and start your application, breaking on the opening brace of your Main function.
This is the typical output of a debug session:

```
> Executing task: dotnet build /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/dotnetcoreapp.csproj <

Microsoft (R) Build Engine version 16.1.54-preview+gd004974104 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 26.68 ms for /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/dotnetcoreapp.csproj.
/home/valter/dotnet-3.0/sdk/3.0.100-preview4-011223/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.RuntimeIdentifierInference.targets(151,5): message NETSDK1057: You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview [/home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/dotnetcoreapp.csproj]
  dotnetcoreapp -> /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/bin/Debug/netcoreapp3.0/dotnetcoreapp.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.31

Terminal will be reused by tasks, press any key to close it.

> Executing task: dotnet publish -o /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/bin/app /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/dotnetcoreapp.csproj <

Microsoft (R) Build Engine version 16.1.54-preview+gd004974104 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 37.83 ms for /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/dotnetcoreapp.csproj.
/home/valter/dotnet-3.0/sdk/3.0.100-preview4-011223/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.RuntimeIdentifierInference.targets(151,5): message NETSDK1057: You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview [/home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/dotnetcoreapp.csproj]
  dotnetcoreapp -> /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/bin/Debug/netcoreapp3.0/dotnetcoreapp.dll
  dotnetcoreapp -> /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/bin/app/

Terminal will be reused by tasks, press any key to close it.

> Executing task: ssh torizon@192.168.1.114  'mkdir -p /home/torizon/app' && rsync -avz /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/bin/app torizon@192.168.1.114:/home/torizon/ <

sending incremental file list
app/
app/dotnetcoreapp
app/dotnetcoreapp.deps.json
app/dotnetcoreapp.dll
app/dotnetcoreapp.pdb
app/dotnetcoreapp.runtimeconfig.json

sent 2,258 bytes  received 811 bytes  6,138.00 bytes/sec
total size is 79,701  speedup is 25.97

Terminal will be reused by tasks, press any key to close it.

> Executing task: ssh torizon@192.168.1.114 'docker stop dotnetcoreapp-dbg ; docker run -d --rm --name dotnetcoreapp-dbg -p 8023:22 -v $(pwd)/app:/app dotnetcoredbg:latest' <

Error response from daemon: No such container: dotnetcoreapp-dbg
e265904ddf731a2c5b9c0cb24556a6f513bea5ce16879541b1bbc84d41c41497

Terminal will be reused by tasks, press any key to close it.

> Executing task: while ! nc -z 192.168.1.114 8023; do sleep 1; done <


Terminal will be reused by tasks, press any key to close it.
```

Message "*Error response from daemon: No such container: dotnetcoreapp-dbg*" will appear if the debug container was not already running, this is normal and will happen the first time you start a debug session after device boot.

You can use F10 to step over instruction, F11 to step into and all the other debugging features provided by Visual Studio Code C# debugger.

### Package and deploy your application

When you are happy about your application and want to run it directly on a Torizon device you will have to build a release container that will include the dotnet core runtime, your application and all his dependencies.  
To do this:
- press **ctrl+P** to open command palette
- type **task buildreleasecontainer** to start build.
- Your application will be built and published and then build of the release container image will start.
- build should complete successfully
    ```
    Successfully built XXXXXXXXX
    Successfully tagged dotnetcore-YYYYYYYY:latest
    ```
- The new image tag will be "dotnetcore-" and the name of your application folder, you will be able to change that by running docker build with a different tag (see chapter "customizing containers").
- press **ctrl+P** again to re-open the command palette.
- type **task runreleasecontainer** to deploy and run your container on the device.
    ```
    > Executing task: dotnet build /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/dotnetcoreapp.csproj <

    Microsoft (R) Build Engine version 16.1.54-preview+gd004974104 for .NET Core
    Copyright (C) Microsoft Corporation. All rights reserved.

    Restore completed in 23.68 ms for /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/dotnetcoreapp.csproj.
    /home/valter/dotnet-3.0/sdk/3.0.100-preview4-011223/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.RuntimeIdentifierInference.targets(151,5): message NETSDK1057: You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview [/home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/dotnetcoreapp.csproj]
    dotnetcoreapp -> /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/bin/Debug/netcoreapp3.0/dotnetcoreapp.dll

    Build succeeded.
        0 Warning(s)
        0 Error(s)

    Time Elapsed 00:00:00.94

    Terminal will be reused by tasks, press any key to close it.

    > Executing task: dotnet publish -o /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/bin/app /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/dotnetcoreapp.csproj <

    Microsoft (R) Build Engine version 16.1.54-preview+gd004974104 for .NET Core
    Copyright (C) Microsoft Corporation. All rights reserved.

    Restore completed in 21.96 ms for /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/dotnetcoreapp.csproj.
    /home/valter/dotnet-3.0/sdk/3.0.100-preview4-011223/Sdks/Microsoft.NET.Sdk/targets/Microsoft.NET.RuntimeIdentifierInference.targets(151,5): message NETSDK1057: You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview [/home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/dotnetcoreapp.csproj]
    dotnetcoreapp -> /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/bin/Debug/netcoreapp3.0/dotnetcoreapp.dll
    dotnetcoreapp -> /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/bin/app/

    Terminal will be reused by tasks, press any key to close it.

    > Executing task: cd /home/valter/Work/Torizon/dot-net-core/dotnetcoreapp/bin/app ; docker build -t dotnetcore-dotnetcoreapp:latest --build-arg APPNAME=dotnetcoreapp.dll -f $templatepath/dotnetcore/Dockerfile . ; cd - <

    Sending build context to Docker daemon  84.53kB
    Step 1/7 : FROM mcr.microsoft.com/dotnet/core/runtime:3.0-stretch-slim-arm32v7
    ---> 6c6f409a2d02
    Step 2/7 : ARG APPNAME
    ---> Using cache
    ---> 302920f14aba
    Step 3/7 : ENV ENVAPPNAME ${APPNAME}
    ---> Using cache
    ---> 1b51d801e909
    Step 4/7 : COPY . /app
    ---> Using cache
    ---> 80d8fbd616ec
    Step 5/7 : WORKDIR /app
    ---> Using cache
    ---> 34b56a0564aa
    Step 6/7 : CMD /usr/bin/dotnet ${ENVAPPNAME}
    ---> Using cache
    ---> 118612c4edd4
    Step 7/7 : EXPOSE 5000
    ---> Using cache
    ---> 5e2feff9d10e
    Successfully built 5e2feff9d10e
    Successfully tagged dotnetcore-dotnetcoreapp:latest

    Terminal will be reused by tasks, press any key to close it.

    > Executing task: docker save dotnetcore-dotnetcoreapp:latest | ssh torizon@192.168.1.114 'docker load' <

    Loaded image: dotnetcore-dotnetcoreapp:latest

    Terminal will be reused by tasks, press any key to close it.

    > Executing task: ssh torizon@192.168.1.114 'docker stop dotnetcoreapp ; docker run -d --rm --name dotnetcoreapp dotnetcore-dotnetcoreapp:latest' <

    Error response from daemon: No such container: dotnetcoreapp
    b676fa3664a2c4573bd15f2333f8b38826b39a9798256bae4270aa656801360d

    Terminal will be reused by tasks, press any key to close it.
    ```

## ASP.NET Core application

Building and debugging an ASP.NET Core application is very similar to the process described in the previous chapters for .NET core applications.
You can use **dotnet new** command to create different kind of ASP.NET core application, depending on what kind of framework and model you want to use.  
In this document we will describe creation of a basic application with HTTP only interface, adding HTTPS involves certificate management and this can be done in different ways using different kind of certificates, depending on specific application needs.  

You should add an additional setting to .vscode\settings.json to set the port that will be used to expose the HTTP server.  

```json
    "toradexdotnetcore.targetHTTPPort": "5000"
```

Also taks.json and launch.json will have some additional settings, related to the extra ports that need to be enable to let your application communicate to the outside. Please check the files in the aspdotnetcore folder of this repository.

You should then build and deploy the debug container as described in previous chapter (ASP.NET Core uses a different base container) and change the application code to use the native (kestrel) http server on the port you configured.

- open **Program.cs** in the Visual Studio Code editor
- add the following code to the **CreateHostBuilder** function (here we use port 5000, as we did in the setting.json sample above):

    ```C#
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>().ConfigureKestrel((context, options) =>
                {
                    options.Listen(IPAddress.Any, 5000);
                });
            });
    ```

You can then press F5 and start debugging your application.  
Debugger won't stop on the entry point (this is not usually very useful for applications that receive requests from clients and just perform basic initialization at startup), but you can place breakpoints in your code as usual.

## Customize containers

.NET Core applications will not run directly on Torizon, they will be hosted inside a container.  
Microsoft provides base containers for .NET Core and ASP.NET Core applications, those need some customization to provide debugging capabilities and to host your application directly inside the container for release builds.  
Inside this repo you will find a folder named "containers", inside the folder you will find four different subfolders:  
- dotnetcore  
This is the basic release container for .NET Core application, it will just integrate your application inside a container and run it when that same container is started.  
Each application will have a different container named dotnetcode-\<your application name>.
- dotnetcoredbg  
This is a generic "host" container providing support (via SSH on port 22) to run and debug .NET core applications. It integrates visual studio debugger and SSH server.
- aspnetcore  
This is release container for ASP.NET Core applications, it includes the runtime, adds the application and runs it at startup, its behaviour is very similar to dotnetcore container, but it's based on a different Microsof-provided base image and exposes also port 5000 for the HTTP interface. You may need to change that or add additional ports that your application may need.
- aspnetcoredbg  
This is the ASP.NET version of dotnetcoredbg container, you may need to change it if you plan to expose different or additional ports.

Those containers can be customized by changing their Dockerfile or by creating a new Dockerfile that uses them in their "FROM" statement.  
Tasks.json and Launch.json reference those containers by the folder name, so you may have to change them if you want to reference a different base container for your application.  
You can add additional exposed port using the EXPOSE clause (this is needed for server ports, not for ports your application accesses as a client) or add additional os components needed by your application.  
Microsoft-provided containers are based on debian stretch, so you can add components invoking the apt package manager.
