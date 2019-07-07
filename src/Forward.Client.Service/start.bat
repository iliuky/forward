@echo off

echo ------------------------------------------
echo -------数据请求转发服务安装管理程序-------
echo ------------------------------------------

set run_path=%cd%
set service_name="Forward.Client.Service"

SC QUERY %service_name% > NUL
IF ERRORLEVEL 1060 GOTO NOTEXIST
GOTO EXIST

:NOTEXIST
sc create %service_name% binpath="%run_path%\Forward.Client.Service.exe"
sc description %service_name% "数据请求转发服务"
sc config %service_name% start= auto

:EXIST
for /f "skip=3 tokens=4" %%i in ('sc query %service_name%') do set "zt=%%i" &goto :next
:next
if /i "%zt%"=="RUNNING" (
    echo 已经发现该服务在运行。
) else (
    echo 启动%service_name% 服务
    sc start %service_name%
)
pause