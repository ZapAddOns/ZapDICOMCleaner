# ZapDICOMCleaner
Program to remove problems from Zap TPS export files to import them in other systems.

Zap plan export in version DP-1007 and DP-1008 of TPS has various problems in at least the RTSTRUCT file, so that files couldn't be imported on some other systems. This programm could open one or more Zip files and repair this problems.

## Problems in RTSTRUCT files
#### DICOM tag (3006,0040)
The coordinates of structures are stored as DS type (string with format "x0\y0\z0\x1\y1\z1...") with max. 16 characters for each floating point. Zap uses sometimes more than 16 characters. The characters over 16 characters are deleted.

#### DICOM tag (3006,002C)
The volume of structures are stored as DS type (string) with max. 16 character for the floating point. Zap uses sometimes more than 16 characters. The characters over 16 characters are deleted.

#### DICOM tag (3006,002A)
The color for structures are stored as DS type (string with format "r\g\b") contain 3 values between 0 an 255. Zap uses 4 values with an additional alpha value. The last value is deleted.

#### DICOM tag (3006,0042)
Some planing system couldn't handle RTSTRUCT files with mixed contour geometric type. Zap exports files with POINT and CLOSED_PLANAR mixed in one structure. All POINT contours are replaced with CLOSED_PLANAR structures. The coordinates are not changed.