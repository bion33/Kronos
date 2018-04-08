#!/bin/bash

# Create detag file
cd (insert working directory here)
python3 Kronos.py

# Move file to hosting directory
sudo mv Kronos_* (insert hosting directory here)

# Set correct permissions to hosting directory
sudo chown -R root:www-data (insert hosting directory here)
sudo chmod -R 450 (insert hosting directory here)