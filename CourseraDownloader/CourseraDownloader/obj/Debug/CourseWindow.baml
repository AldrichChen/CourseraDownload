   M S B A M L   `   `   `  ����  CourseraDownloader%    CourseraDownloader.CourseWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          �Name$  CourseWindow1��5   	    � Title$ Coursera Downloader��5   	   $	��430��6%   $	��650��62    � Icon$ Resources/coursera.png��6>    � 
ResizeMode$ CanMinimize=�6\    � 	Resources 5      &   System.Windows.Controls.DataGrid �� 
TargetType �� Property �� Value��%R   '        �� 5   
   "  6   .�5      ��  � ��$ 14(�6)   5      5	      .��5
      � 
������� � RowDefinitions 5   
   �� 5      $��5 �6   �� 5      $��1* �6   �� 5      $��5 �6   �� 5      $��8* �6   �� 5      $��5 �6   �� 5      $��1* �6   �� 5      $��5 �6   	 � ColumnDefinitions	 5   
   �� 5      $��5 �6   �� 5      $
��3.5* �6   �� 5      $��5 �6   �� 5      $��3* �6   �� 5      $��5 �6   �� 5      $
��3.5* �6   �� 5      $��5 �6   �� 5      $
��1.5* �6   �� 5      $��5 �6   �� 5      $
��1.6* �6   �� 5      $��5 �6   .+�5"   
   �� -   5        
 ��Name$
 
greetLabel��5"      $��1��6$   $��1��61   $�� ��6A   $��Center=�6L   $��Center=�6i   $��18(�6�   �� 5#   
   -   5         ��Name$ courseComboBox��5#      $��1��6+   $��3��68   $��3��6H    �� Text$ Select course ...��6\    �� 
IsEditable .  �� 
IsReadOnly . $��Stretch=�6�   $��Center=�6�   $��14(�6�   6�   �� 5$   
   -   5        $ unitComboBox��5$      $��1��6)   $��7��66   $��3��6F   $ Select unit ...��6Z    .  . $��Stretch=�6�   $��Center=�6�   $��14(�6�   6�     5%   
   -   5          Name$ materialDataGrid��5%      $��3��6-   $��1��6:   $��9��6J      AutoGenerateColumns .  $��Stretch=�6z   $��Stretch=�6�      Columns 5&      4  .System.Windows.Controls.DataGridTemplateColumn  5'      ,  &System.Windows.Controls.DataGridColumn   CanUserReorder .     CanUserResize .     Width5  /System.Windows.Controls.DataGridLengthConverter$ 5* 6V      HeaderTemplate 5(      �� 5)      .R�5*      �� $��Title��6(   5+         CellTemplate 5-      �� 5.      .R�5/      �� ��6(   ��  �� Path$ Title� � ToolTip 6D   �� $ Title�50      52        53       .   .  $ 1* 6V      HeaderStyle 54      �� 55      =  7System.Windows.Controls.Primitives.DataGridColumnHeader"  6    .�56      ��  � ��$ Center=�6K   57       59      �� 5:      .R�5;      ��  ��Name$ selectAllCheckBox��6'   -   5        $�� Select��5;   B   $��14(�6T   6�   5<       5>      �� 5?      .R�5@      �� $��Center=�6y   $��Center=�6�    d� 	IsChecked 6'   �� $ 
IsSelected� �� Mode$ TwoWay=� �� UpdateSourceTrigger$ 	LostFocus=�6�   5A      5C      5E      � 5F   
   -   5         �Name$ downloadProgressBar��5F      $��5��63   $��1��6@   $��3��6P   $��Stretch=�6d   $��Center=�6�   $��20��6�   �� 5G   
   -   5         ��Name$ downloadPathTextBox��5G      $��5��6/   $��5��6<     �� 
IsReadOnly  . $��Stretch=�6^   $��Center=�6|   $��14(�6�   �� 5H   
   -	   5        ! ��Name$! browseButton��5H      $��5��6'   $��7��64   $��Browse��6D   $��Stretch=�6U   $��Center=�6s   $��14(�6�   6�   �� 5I   
   -
   5        $! downloadButton��5I      $��5��6)   $��9��66   $��Download��6F   $��Stretch=�6Y   $��Center=�6w   $��14(�6�   6�   5J      5K      