@echo off
:: 실행할 프로그램이 있는 디렉터리로 이동
cd /d "D:\gitwork\TradingviewAlertBot\Build\windows"

:: 종료할 프로그램 이름 설정 (예: notepad.exe)
set PROGRAM_NAME=TradingviewAlertBot.exe

:: 실행할 파일 경로 설정 (예: C:\Program Files\App\app.exe)
set FILE_TO_RUN="D:\gitwork\TradingviewAlertBot\Build\windows\TradingviewAlertBot.exe"

:: 프로그램 강제 종료
echo %PROGRAM_NAME% 종료 중...
taskkill /F /IM %PROGRAM_NAME% 2>nul

:: 실행할 파일 실행
echo %FILE_TO_RUN% 실행 중...
start "" %FILE_TO_RUN% mailaddress=ljwjapan@gmail.com mailpassword=gxzo:zggu:btza:wfye apikey=d3dXOHzDCraHfp86DV apisecret=w6rOqxShoaKxKB9IxEgJI9Q3oCnThwSg8Eng