import os
import time
import shutil

logs = os.listdir("./bin/Debug/netcoreapp2.0/Logs")
for ll in logs:
    os.remove("./bin/Debug/netcoreapp2.0/Logs/"+ll)
print("Cleaned logs : "+str(len(logs)))

def createEnv(forport,istest=False):
    at = "./Environments/"+str(forport)
    print("Creating env:"+str(forport))
    if (os.path.exists(at)):
        shutil.rmtree(at)

    shuf = ""
    if (istest):
        shuf = "-test"

    shutil.copytree("./bin/Debug/netcoreapp2.0",at)
    os.popen("start dotnet "+str(at)+"/MemeIum.dll "+str(forport)+" "+shuf)

createEnv(1232)
createEnv(2233)

