#!/bin/sh
aws dynamodb delete-table \
    --endpoint http://localhost:8000 \
    --table-name Simulacrum
