<?xml version="1.0"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:template match="/Dormitory">
		<html>
			<body>
				<h2>Student List</h2>
				<table border="1">
					<tr>
						<th>Name</th>
						<th>Faculty</th>
						<th>Department</th>
						<th>Room</th>
					</tr>
					<xsl:for-each select="Student">
						<tr>
							<td>
								<xsl:value-of select="Name"/>
							</td>
							<td>
								<xsl:value-of select="@Faculty"/>
							</td>
							<td>
								<xsl:value-of select="@Department"/>
							</td>
							<td>
								<xsl:value-of select="@Room"/>
							</td>
						</tr>
					</xsl:for-each>
				</table>
			</body>
		</html>
	</xsl:template>
</xsl:stylesheet>
