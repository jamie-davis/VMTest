set /p Increment=Really increment the revision number for VMTest? Enter Y to proceed: 
if %Increment%==Y tools\setcsprojver .. /i:fv,b /env:consoletoolsversion
if %Increment%==Y tools\setcsprojver .. /i:av,b /env:VMTestVersion

pause