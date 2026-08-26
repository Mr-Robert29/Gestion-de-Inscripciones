"""
    Para automatizar el push de un repositorio de git, se puede utilizar la libreria gitpython
cual permite interactuar con repositorios de git desde python.
    Mediante el comando:
    pip install gitpython
    Desde la terminal.
    versiones:
    
    -   Version 3.14.6 de python
    -   Version 2.49.0.windows.1 de git
"""
import git
def push():
    repo = git.Repo('.') #aca se manda el directorio del repositorio
    repo.git.add('--all') # se agrega todo para luego hacer commit
    try:
        nombreCommit = input("Ingrese el nombre del commit: ") #se pide el nombre del commit
        repo.index.commit(nombreCommit) #nombre del commit
        origin = repo.remote(name='origin') #nombre del remoto, en este caso origin
        origin.push() #push 
    except Exception as e:
        print(f"Ocurrio este error: /n {e}")
push() #llamado a la funcion push