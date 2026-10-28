pipeline {
    agent any

    options {
        timestamps()
        timeout(time: 30, unit: 'MINUTES')
        disableConcurrentBuilds()
    }

    environment {
        PYTHON_IMAGE = "antiscam-python-api:${BUILD_NUMBER}"
        BLOG_IMAGE = "antiscam-blog-api:${BUILD_NUMBER}"
    }

    stages {
        stage('Python tests') {
            steps {
                sh '''
                    python3 -m venv .venv-ci
                    .venv-ci/bin/python -m pip install --upgrade pip
                    .venv-ci/bin/python -m pip install -r requirements-dev.txt
                    .venv-ci/bin/python -m pip install -e .
                    .venv-ci/bin/python train.py
                    .venv-ci/bin/python -m pytest --junitxml=pytest-results.xml
                '''
            }
            post {
                always {
                    junit allowEmptyResults: true, testResults: 'pytest-results.xml'
                }
            }
        }

        stage('.NET tests') {
            steps {
                sh '''
                    CI_SQL_PASSWORD="$(python3 -c 'import secrets; print(secrets.token_hex(20) + "Aa1!")')"
                    docker run -d --name antiscam-ci-sql \
                        -e ACCEPT_EULA=Y -e MSSQL_PID=Developer \
                        -e "MSSQL_SA_PASSWORD=$CI_SQL_PASSWORD" \
                        -p 127.0.0.1::1433 mcr.microsoft.com/mssql/server:2022-latest
                    SQL_PORT=$(docker port antiscam-ci-sql 1433/tcp | awk -F: '{print $NF}')
                    for attempt in $(seq 1 60); do
                        if docker exec antiscam-ci-sql /opt/mssql-tools18/bin/sqlcmd \
                            -S localhost -U sa -P "$CI_SQL_PASSWORD" -C -Q 'SELECT 1' -b >/dev/null 2>&1; then
                            break
                        fi
                        if [ "$attempt" -eq 60 ]; then
                            docker logs antiscam-ci-sql
                            exit 1
                        fi
                        sleep 5
                    done
                    export ANTISCAM_TEST_SQLSERVER="Server=127.0.0.1,$SQL_PORT;User Id=sa;Password=$CI_SQL_PASSWORD;Encrypt=True;TrustServerCertificate=True;Connect Timeout=5"
                    dotnet test AntiScamBlog.sln --configuration Release --logger 'trx;LogFileName=dotnet-tests.trx'
                '''
            }
            post {
                always {
                    sh 'docker rm -f antiscam-ci-sql || true'
                }
            }
        }

        stage('Build application images') {
            steps {
                sh 'docker build -f ci/python/Dockerfile -t "$PYTHON_IMAGE" .'
                sh 'docker build -f ci/dotnet/Dockerfile -t "$BLOG_IMAGE" .'
            }
        }
    }

    post {
        always {
            deleteDir()
        }
    }
}
