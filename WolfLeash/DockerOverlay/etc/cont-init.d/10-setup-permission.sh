#!/usr/bin/env bash

set -e

chown ${APP_UID} -R /app
chown ${APP_UID} -R /home/app