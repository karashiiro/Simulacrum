#!/bin/sh
aws dynamodb delete-table \
    --endpoint http://localhost:8000 \
    --no-cli-pager \
    --table-name Simulacrum
