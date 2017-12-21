import os
import time

logs = os.listdir("./Logs")
for ll in logs:
    os.remove("./Logs/"+ll)
print("Cleaned logs : "+str(len(logs)))

os.popen("start dotnet ./bin/Debug/netcoreapp2.0/MemeIum.dll 3232")
time.sleep(1)
os.popen("start dotnet ./bin/Debug/netcoreapp2.0/MemeIum.dll 4242 -test")
